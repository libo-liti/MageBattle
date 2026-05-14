using UnityEngine;

public static class Constant
{
    public enum GameState { MainMenu, Playing, Pause, GameOver }
    
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
        // 한 손 포즈 (사용 안할수도 있지만 혹시나 해서 보관)
        Fist, Pointing, ThumbUp, Pinky, Victory, RockSign, Three, OpenHand,   
        
        // 원소
        Land, Wind, Fire, Water,
        
        // 형태
        Attack, Defense, Special
    }

    public enum BattleState
    {
        Idle,
        ElementCharging,
        FormCharging,
        Casting
    }

    public enum BgmId
    {
        None,
        MainMenu,
        Battle,
        Victory,
        Defeat
    }

    public enum SfxId
    {
        None,
    
        // UI
        UiClick,
        UiHover,
    
        // 영창
        ElementConfirmed,    // 원소 1초 유지 성공
        FormConfirmed,       // 형태 1초 유지 성공
    
        // 마법 발사
        CastFire,
        CastWater,
        CastWind,
        CastLand,
    
        // 결과
        Hit,                 // 데미지 받음
        Block,               // 방어 성공
        Counter,             // 카운터 성공
        RoundFail,           // 영창 실패 (시간 초과)
    
        // 게임 결과
        Victory,
        Defeat,
    }
}
