using System.Net.WebSockets;

namespace Miko.Components.Vnc.Protocol;

/// <summary>Binary RFB is a byte stream: WebSocket message/fragment boundaries have no protocol meaning.</summary>
internal sealed class VncWebSocketStream(ClientWebSocket socket) : Stream
{
    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length == 0) return 0;
        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close) return 0;
            if (result.MessageType != WebSocketMessageType.Binary) throw new InvalidDataException("Expected binary RFB data.");
            if (result.Count > 0) return result.Count;
        }
    }
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => socket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);
    protected override void Dispose(bool disposing) { if (disposing) socket.Dispose(); base.Dispose(disposing); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("WebSocket reads must be asynchronous.");
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("WebSocket writes must be asynchronous.");
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
