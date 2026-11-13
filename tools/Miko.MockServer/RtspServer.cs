using System.Buffers.Binary;
using System.Globalization;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Miko.MockServer;

/// <summary>RTSP 1.0, H.264/RTP (RFC 6184), interleaved TCP and RTCP sender reports.</summary>
public sealed class RtspServer(MockOptions options, MediaLibrary media, ILogger<RtspServer> logger)
    : TcpServer(options, options.RtspPort, logger)
{
    protected override Task ServeAsync(TcpClient client, CancellationToken token) => new Session(client.GetStream(), media).RunAsync(token);

    private sealed class Session(NetworkStream stream, MediaLibrary media)
    {
        private const string Methods = "OPTIONS, DESCRIBE, SETUP, PLAY, PAUSE, GET_PARAMETER, SET_PARAMETER, TEARDOWN";
        private readonly SemaphoreSlim _write = new(1, 1);
        private readonly string _id = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        private readonly uint _ssrc = BinaryPrimitives.ReadUInt32BigEndian(RandomNumberGenerator.GetBytes(4));
        private ushort _sequence = (ushort)RandomNumberGenerator.GetInt32(65536);
        private uint _timestamp = BinaryPrimitives.ReadUInt32BigEndian(RandomNumberGenerator.GetBytes(4));
        private uint _packets, _octets;
        private byte _rtpChannel, _rtcpChannel = 1;
        private bool _setup;
        private CancellationTokenSource? _playStop;
        private Task? _play;

        public async Task RunAsync(CancellationToken token)
        {
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            try
            {
                while (!lifetime.IsCancellationRequested)
                {
                    using var idle = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                    idle.CancelAfter(TimeSpan.FromSeconds(90));
                    var request = await ReadRequest(idle.Token);
                    if (request == null) return;
                    if (!request.Headers.TryGetValue("CSeq", out string? cseq) || !int.TryParse(cseq, out int sequence) || sequence < 0)
                    { await Reply("0", 400, "Bad Request", token); return; }
                    if (request.Uri != "*" && (!Uri.TryCreate(request.Uri, UriKind.Absolute, out var uri) || uri.Scheme != "rtsp" || uri.AbsolutePath.TrimEnd('/') is not ("/webcam" or "/webcam/trackID=0")))
                    { await Reply(cseq, 404, "Not Found", token); continue; }
                    if (request.Method is "PLAY" or "PAUSE" or "TEARDOWN" or "GET_PARAMETER" or "SET_PARAMETER")
                    {
                        if (!_setup || request.Headers.GetValueOrDefault("Session")?.Split(';')[0].Trim() != _id)
                        { await Reply(cseq, 454, "Session Not Found", token); continue; }
                    }
                    switch (request.Method)
                    {
                        case "OPTIONS":
                            await Reply(cseq, 200, "OK", token, $"Public: {Methods}\r\n"); break;
                        case "DESCRIBE":
                            string baseUri = request.Uri.Replace("/trackID=0", "", StringComparison.Ordinal).TrimEnd('/') + "/";
                            string profile = Convert.ToHexString(media.Sps.AsSpan(1, 3));
                            string sdp = $"v=0\r\no=- 1 1 IN IP4 127.0.0.1\r\ns=Miko panorama\r\nc=IN IP4 0.0.0.0\r\nt=0 0\r\na=control:*\r\na=range:npt=0-\r\nm=video 0 RTP/AVP 96\r\na=rtpmap:96 H264/90000\r\na=fmtp:96 packetization-mode=1;profile-level-id={profile};sprop-parameter-sets={Convert.ToBase64String(media.Sps)},{Convert.ToBase64String(media.Pps)}\r\na=framerate:5\r\na=control:trackID=0\r\n";
                            await Reply(cseq, 200, "OK", token, $"Content-Type: application/sdp\r\nContent-Base: {baseUri}\r\n", sdp); break;
                        case "SETUP":
                            if (_setup) { await Reply(cseq, 455, "Method Not Valid in This State", token); break; }
                            if (!TryTransport(request.Headers.GetValueOrDefault("Transport"), out _rtpChannel, out _rtcpChannel))
                            { await Reply(cseq, 461, "Unsupported Transport", token); break; }
                            _setup = true;
                            await Reply(cseq, 200, "OK", token, $"Transport: RTP/AVP/TCP;unicast;interleaved={_rtpChannel}-{_rtcpChannel};ssrc={_ssrc:X8}\r\n"); break;
                        case "PLAY":
                            if (request.Headers.TryGetValue("Range", out string? range) && range is not ("npt=0-" or "npt=0.000-" or "npt=now-"))
                            { await Reply(cseq, 457, "Invalid Range", token); break; }
                            await Reply(cseq, 200, "OK", token, $"Range: npt=0.000-\r\nRTP-Info: url={request.Uri.Replace("/trackID=0", "", StringComparison.Ordinal).TrimEnd('/')}/trackID=0;seq={_sequence};rtptime={_timestamp}\r\n");
                            if (_play == null)
                            {
                                _playStop = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                                _play = SendVideo(_playStop.Token, lifetime);
                            }
                            break;
                        case "PAUSE":
                            await StopVideo(); await Reply(cseq, 200, "OK", token); break;
                        case "GET_PARAMETER":
                        case "SET_PARAMETER":
                            await Reply(cseq, 200, "OK", token); break;
                        case "TEARDOWN":
                            await StopVideo(); await Reply(cseq, 200, "OK", token); return;
                        default: await Reply(cseq, 405, "Method Not Allowed", token, $"Allow: {Methods}\r\n"); break;
                    }
                }
            }
            finally { await StopVideo(); _write.Dispose(); }
        }

        private async Task StopVideo()
        {
            if (_playStop == null) return;
            await _playStop.CancelAsync();
            try { await _play!; } catch (OperationCanceledException) { }
            finally { _playStop.Dispose(); _playStop = null; _play = null; }
        }

        private async Task SendVideo(CancellationToken token, CancellationTokenSource lifetime)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1.0 / MediaLibrary.FramesPerSecond));
                int frameIndex = 0;
                do
                {
                    var frame = media.Frames[frameIndex % media.Frames.Count];
                    for (int i = 0; i < frame.Length; i++) await SendNal(frame[i], i == frame.Length - 1, token);
                    if (frameIndex % 25 == 0) await SendReport(token);
                    _timestamp = unchecked(_timestamp + 90000 / MediaLibrary.FramesPerSecond);
                    frameIndex++;
                } while (await timer.WaitForNextTickAsync(token));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
            { await lifetime.CancelAsync(); }
        }

        private async Task SendNal(byte[] nal, bool marker, CancellationToken token)
        {
            const int maxPayload = 1200;
            if (nal.Length <= maxPayload) { await SendRtp(nal, marker, token); return; }
            for (int offset = 1; offset < nal.Length;)
            {
                int length = Math.Min(maxPayload - 2, nal.Length - offset);
                bool last = offset + length == nal.Length;
                var fragment = new byte[length + 2];
                fragment[0] = (byte)((nal[0] & 0xe0) | 28);
                fragment[1] = (byte)((nal[0] & 31) | (offset == 1 ? 0x80 : 0) | (last ? 0x40 : 0));
                nal.AsSpan(offset, length).CopyTo(fragment.AsSpan(2));
                await SendRtp(fragment, marker && last, token);
                offset += length;
            }
        }

        private async Task SendRtp(byte[] payload, bool marker, CancellationToken token)
        {
            var packet = new byte[12 + payload.Length]; packet[0] = 0x80; packet[1] = (byte)(96 | (marker ? 0x80 : 0));
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), _sequence++);
            Write32(packet, 4, _timestamp); Write32(packet, 8, _ssrc); payload.CopyTo(packet, 12);
            await Interleave(_rtpChannel, packet, token);
            _packets++; _octets += (uint)payload.Length;
        }

        private async Task SendReport(CancellationToken token)
        {
            // Compound RTCP: SR + SDES CNAME (RFC 3550).
            byte[] cname = Encoding.ASCII.GetBytes("miko-mock");
            int sdesLength = (4 + 4 + 2 + cname.Length + 1 + 3) & ~3;
            var packet = new byte[28 + sdesLength]; packet[0] = 0x80; packet[1] = 200; packet[3] = 6;
            Write32(packet, 4, _ssrc);
            double ntp = (DateTimeOffset.UtcNow - new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds;
            Write32(packet, 8, (uint)ntp); Write32(packet, 12, (uint)((ntp - Math.Floor(ntp)) * 4294967296.0));
            Write32(packet, 16, _timestamp); Write32(packet, 20, _packets); Write32(packet, 24, _octets);
            packet[28] = 0x81; packet[29] = 202;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(30), (ushort)(sdesLength / 4 - 1));
            Write32(packet, 32, _ssrc); packet[36] = 1; packet[37] = (byte)cname.Length; cname.CopyTo(packet, 38);
            await Interleave(_rtcpChannel, packet, token);
        }

        private async Task Interleave(byte channel, byte[] packet, CancellationToken token)
        {
            var bytes = new byte[4 + packet.Length]; bytes[0] = (byte)'$'; bytes[1] = channel;
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2), (ushort)packet.Length); packet.CopyTo(bytes, 4);
            await Write(bytes, token);
        }

        private async Task Reply(string cseq, int status, string reason, CancellationToken token, string headers = "", string body = "")
        {
            string session = _setup ? $"Session: {_id};timeout=90\r\n" : "";
            await Write(Encoding.UTF8.GetBytes($"RTSP/1.0 {status} {reason}\r\nCSeq: {cseq}\r\nServer: Miko.MockServer\r\n{session}{headers}Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}"), token);
        }

        private async Task Write(byte[] bytes, CancellationToken token)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            await _write.WaitAsync(timeout.Token);
            try { await stream.WriteAsync(bytes, timeout.Token); }
            finally { _write.Release(); }
        }

        private sealed record Request(string Method, string Uri, Dictionary<string, string> Headers);

        private async Task<Request?> ReadRequest(CancellationToken token)
        {
            var single = new byte[1];
            while (true)
            {
                if (await stream.ReadAsync(single, token) == 0) return null;
                if (single[0] != '$') break;
                var header = new byte[3]; await stream.ReadExactlyAsync(header, token);
                var rtcp = new byte[BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(1))];
                await stream.ReadExactlyAsync(rtcp, token); // Client receiver reports.
            }
            var buffer = new List<byte> { single[0] };
            while (buffer.Count < 16384)
            {
                await stream.ReadExactlyAsync(single, token); buffer.Add(single[0]);
                int n = buffer.Count;
                if (n >= 4 && buffer[n - 4] == 13 && buffer[n - 3] == 10 && buffer[n - 2] == 13 && buffer[n - 1] == 10) break;
            }
            if (buffer.Count >= 16384) throw new InvalidDataException("RTSP headers too large.");
            var lines = Encoding.ASCII.GetString(buffer.ToArray()).Split("\r\n");
            var start = lines[0].Split(' ');
            if (start.Length != 3 || start[2] != "RTSP/1.0") throw new InvalidDataException("Invalid RTSP request line.");
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines.Skip(1).Where(l => l.Length > 0))
            {
                int colon = line.IndexOf(':');
                if (colon <= 0 || !headers.TryAdd(line[..colon].Trim(), line[(colon + 1)..].Trim())) throw new InvalidDataException("Invalid/duplicate RTSP header.");
            }
            if (headers.TryGetValue("Content-Length", out var length))
            {
                if (!int.TryParse(length, NumberStyles.None, CultureInfo.InvariantCulture, out int count) || count > 16384) throw new InvalidDataException("RTSP body too large.");
                await stream.ReadExactlyAsync(new byte[count], token);
            }
            return new(start[0], start[1], headers);
        }

        private static bool TryTransport(string? value, out byte rtp, out byte rtcp)
        {
            rtp = 0; rtcp = 1;
            if (value == null) return false;
            var parts = value.Split(';', StringSplitOptions.TrimEntries);
            if (!parts[0].Equals("RTP/AVP/TCP", StringComparison.OrdinalIgnoreCase) || parts.Contains("multicast", StringComparer.OrdinalIgnoreCase)) return false;
            var channels = parts.FirstOrDefault(p => p.StartsWith("interleaved=", StringComparison.OrdinalIgnoreCase))?[12..].Split('-');
            return channels is { Length: 2 } && byte.TryParse(channels[0], out rtp) && byte.TryParse(channels[1], out rtcp) && rtp != rtcp;
        }

        private static void Write32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset), value);
    }
}
