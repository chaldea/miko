using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Miko.MockServer;

public abstract class TcpServer(MockOptions options, int port, ILogger logger) : BackgroundService
{
    private readonly TcpListener _listener = new(IPAddress.Parse(options.ListenAddress), port);
    private readonly ConcurrentDictionary<TcpClient, Task> _clients = new();
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        logger.LogInformation("{Server} listening on {Port}", GetType().Name, Port);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                if (_clients.Count >= options.MaxConnections) { client.Dispose(); continue; }
                client.NoDelay = true;
                // Register before running, including sessions that disconnect immediately.
                var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _clients[client] = completion.Task;
                _ = RunClientAsync(client, completion, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            _listener.Stop();
            foreach (var client in _clients.Keys) client.Dispose();
            await Task.WhenAll(_clients.Values);
        }
    }

    private async Task RunClientAsync(TcpClient client, TaskCompletionSource completion, CancellationToken token)
    {
        try { await ServeAsync(client, token); }
        catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or ObjectDisposedException or InvalidDataException)
        { logger.LogDebug(ex, "{Server} client disconnected", GetType().Name); }
        catch (Exception ex) { logger.LogError(ex, "{Server} session failed", GetType().Name); }
        finally { client.Dispose(); _clients.TryRemove(client, out _); completion.TrySetResult(); }
    }

    protected abstract Task ServeAsync(TcpClient client, CancellationToken token);

    public override void Dispose() { _listener.Stop(); base.Dispose(); }
}
