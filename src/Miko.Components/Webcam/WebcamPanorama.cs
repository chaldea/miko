using Miko.Common;

namespace Miko.Components.Webcam;

/// <summary>Client-side PTZ over a panorama. The normalized crop stays inside the source image.</summary>
public sealed class WebcamPanorama
{
    private readonly object _gate = new();
    private readonly TimeProvider _clock;
    private long _last;
    private double _x = .5, _y = .5, _height = .75;
    private PtzDirection? _direction;
    private double _speed;

    public WebcamPanorama(TimeProvider? clock = null) { _clock = clock ?? TimeProvider.System; _last = _clock.GetTimestamp(); }

    /// <summary>A 16:9 field of view within the 1280×360 source, in normalized coordinates.</summary>
    public RectF Crop
    {
        get
        {
            lock (_gate)
            {
                Advance();
                return new RectF((float)(_x - _height / 4), (float)(_y - _height / 2), (float)(_height / 2), (float)_height);
            }
        }
    }

    internal void Start(PtzDirection direction, PtzSpeed speed)
    {
        if (!Enum.IsDefined(direction) || !Enum.IsDefined(speed)) throw new ArgumentOutOfRangeException(nameof(direction));
        lock (_gate) { Advance(); _direction = direction; _speed = speed switch { PtzSpeed.Slow => .12, PtzSpeed.Fast => .5, _ => .25 }; }
    }

    internal void Stop(PtzDirection direction) { lock (_gate) { Advance(); if (_direction == direction) _direction = null; } }

    private void Advance()
    {
        long now = _clock.GetTimestamp();
        double step = _clock.GetElapsedTime(_last, now).TotalSeconds * _speed;
        _last = now;
        if (_direction is PtzDirection.Left or PtzDirection.UpLeft or PtzDirection.DownLeft) _x -= step;
        if (_direction is PtzDirection.Right or PtzDirection.UpRight or PtzDirection.DownRight) _x += step;
        if (_direction is PtzDirection.Up or PtzDirection.UpLeft or PtzDirection.UpRight) _y -= step;
        if (_direction is PtzDirection.Down or PtzDirection.DownLeft or PtzDirection.DownRight) _y += step;
        if (_direction == PtzDirection.ZoomIn) _height -= step;
        if (_direction == PtzDirection.ZoomOut) _height += step;
        _height = Math.Clamp(_height, .25, 1);
        _x = Math.Clamp(_x, _height / 4, 1 - _height / 4);
        _y = Math.Clamp(_y, _height / 2, 1 - _height / 2);
    }
}
