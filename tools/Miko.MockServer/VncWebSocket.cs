using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace Miko.MockServer;

/// <summary>A binary RFB WebSocket bridge to this process's mock desktop, never an arbitrary proxy.</summary>
internal static class VncWebSocket
{
    public static async Task HandleAsync(HttpContext context, VncServer server, MockOptions options, IHostApplicationLifetime lifetime)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Use a binary RFB WebSocket connection.");
            return;
        }
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, lifetime.ApplicationStopping);
        var address = IPAddress.Parse(options.ListenAddress);
        if (address.Equals(IPAddress.Any)) address = IPAddress.Loopback;
        if (address.Equals(IPAddress.IPv6Any)) address = IPAddress.IPv6Loopback;
        using var tcp = new TcpClient { NoDelay = true };
        await tcp.ConnectAsync(address, server.Port, cancellation.Token);
        using var socket = await context.WebSockets.AcceptWebSocketAsync(
            context.WebSockets.WebSocketRequestedProtocols.Contains("binary") ? "binary" : null);
        var stream = tcp.GetStream();
        var receive = ReceiveAsync();
        var send = SendAsync();
        try
        {
            await await Task.WhenAny(receive, send);
        }
        catch (Exception ex) when (ex is IOException or WebSocketException or OperationCanceledException) { }
        finally
        {
            await cancellation.CancelAsync();
            tcp.Dispose();
            socket.Abort();
            try { await Task.WhenAll(receive, send); }
            catch (Exception ex) when (ex is IOException or WebSocketException or OperationCanceledException or ObjectDisposedException) { }
        }

        async Task ReceiveAsync()
        {
            var buffer = new byte[16 * 1024];
            while (true)
            {
                var result = await socket.ReceiveAsync(buffer.AsMemory(), cancellation.Token);
                if (result.MessageType == WebSocketMessageType.Close) return;
                if (result.MessageType != WebSocketMessageType.Binary)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.InvalidMessageType, "Binary RFB required", cancellation.Token);
                    return;
                }
                await stream.WriteAsync(buffer.AsMemory(0, result.Count), cancellation.Token);
            }
        }

        async Task SendAsync()
        {
            var buffer = new byte[64 * 1024];
            while (true)
            {
                int count = await stream.ReadAsync(buffer, cancellation.Token);
                if (count == 0) return;
                await socket.SendAsync(buffer.AsMemory(0, count), WebSocketMessageType.Binary, true, cancellation.Token);
            }
        }
    }
}
