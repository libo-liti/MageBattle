public readonly struct FingerStates
{
    public readonly bool index, middle, ring, pinky;
    public bool OnlyIndex => index && !middle && !ring && !pinky;
    public bool ThreeFingers => index && middle && ring && !pinky;
    public bool AllExtended => index && middle && ring && pinky;
    public bool AllFolded => !index && !middle && !ring && !pinky;
    
    public FingerStates(bool index, bool middle, bool ring, bool pinky)
    {
        this.index = index;
        this.middle = middle;
        this.ring = ring;
        this.pinky = pinky;
    }
}
