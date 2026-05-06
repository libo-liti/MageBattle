using UnityEngine;
using static Constant;

public class RoundManager
{
    private CasterContext _player;
    private AICaster _ai;

    public RoundManager(CasterContext player, AICaster ai)
    {
        _player = player;
        _ai = ai;
    }

    public void Update()
    {
        if(_player.state == BattleState.Casting && _ai.ctx.state == BattleState.Casting)
            ResolveRound();
    }

    private void ResolveRound()
    {
        Debug.Log("=== 라운드 결과 ===");
        Debug.Log($"Player: {_player.confirmedElement} + {_player.confirmedForm}");
        Debug.Log($"AI : {_ai.ctx.confirmedElement} + {_ai.ctx.confirmedForm}");
        
        ResetCasters();
    }

    private void ResetCasters()
    {
        _player.state = BattleState.Idle;
        _player.confirmedElement = HandPose.Unknown;
        _player.confirmedForm = HandPose.Unknown;
        _player.chargingElement = HandPose.Unknown;
        _player.chargingForm = HandPose.Unknown;
        _player.totalTime = 0f;
        _player.holdTime = 0f;

        _ai.Reset();
    }
}
