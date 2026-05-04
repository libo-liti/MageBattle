using UnityEngine;

public static class Constant
{
    public enum HandJoint
    {
        Wrist,           // 0 (Body wrist position)
        ThumbCmc,       // 1
        ThumbMcp,       // 2
        ThumbIP,        // 3
        ThumbTip,       // 4
        IndexMcp,       // 5
        IndexPip,       // 6
        IndexDip,       // 7
        IndexTip,       // 8
        MiddleMcp,      // 9
        MiddlePip,      // 10
        MiddleDip,      // 11
        MiddleTip,      // 12
        RingMcp,        // 13
        RingPip,        // 14
        RingDip,        // 15
        RingTip,        // 16
        PinkyMcp,       // 17
        PinkyPip,       // 18
        PinkyDip,       // 19
        PinkyTip // 21 (Original hand wrist position from MediaPipe)
    }
    
    public enum HandPose
    {
        Unknown,
        Fist,       // 주먹
        Pointing,   // 검지만
        ThumbUp,    // 엄지만
        Pinky,       // 소지
        Victory,    // V포즈
        RockSign,   // 검지, 소지
        Three,      // 검지, 중지, 약지
        OpenHand,   // 손 전체
        Land,
        Wind,
        Fire,
        Water
    }
}
