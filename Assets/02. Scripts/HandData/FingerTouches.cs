public readonly struct FingerTouches
{
    private readonly bool _index, _middle, _ring, _pinky, _indexPip;
    public bool OnlyIndexTip => _index && !_middle && !_ring && !_pinky && !_indexPip;
    public bool OnlyIndexPip => !_index && !_middle && !_ring && !_pinky && _indexPip;
    public bool AllTouches => _index && _middle && _ring && _pinky;
    private bool AnyTouched => _index || _middle || _ring || _pinky || _indexPip;
    public bool NoneTouched => !AnyTouched;

    public FingerTouches(bool index, bool middle, bool ring, bool pinky, bool indexPip)
    {
        _index = index;
        _middle = middle;
        _ring = ring;
        _pinky = pinky;
        _indexPip = indexPip;
    }
}