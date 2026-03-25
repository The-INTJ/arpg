namespace ARPG;

public partial class ForwardDoubleTapDetector
{
    private readonly double _tapWindowSeconds;
    private double _lastTapTime = double.NegativeInfinity;
    private bool _armed;

    public ForwardDoubleTapDetector(double tapWindowSeconds = 0.28)
    {
        _tapWindowSeconds = tapWindowSeconds;
    }

    public bool IsBurstActive(bool forwardHeld)
    {
        return _armed && forwardHeld;
    }

    public void Update(bool forwardJustPressed, bool forwardHeld, double elapsedSeconds)
    {
        if (forwardJustPressed)
        {
            _armed = elapsedSeconds - _lastTapTime <= _tapWindowSeconds;
            _lastTapTime = elapsedSeconds;
        }

        if (!forwardHeld)
            _armed = false;
    }

    public void Reset()
    {
        _armed = false;
        _lastTapTime = double.NegativeInfinity;
    }
}
