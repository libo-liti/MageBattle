using System;
using UnityEngine;
using static Constant;
using static DamageCalculator;

public class RoundManager
{
    public event Action<RoundResult> OnRoundResolved; 
    
    private CasterContext _player;
    private AICaster _ai;

    private int _playerHp = 100;
    private int _aiHp = 100;
    private HandPose _prevPlayerEl = HandPose.Unknown;
    private HandPose _prevAiEl = HandPose.Unknown;
    private bool _gameOver = false;
    private bool _roundActive = false;
    private float _roundElapsedTime = 0f;
    public const float ROUND_TIMEOUT = 10F;

    public float RoundElapsedTime => _roundElapsedTime;
    
    // OnGUI
    public int PlayerHp => _playerHp;
    public int AiHp => _aiHp;
    public bool GameOver => _gameOver;

    public RoundManager(CasterContext player, AICaster ai)
    {
        _player = player;
        _ai = ai;
    }

    public void Update(float deltaTime)
    {
        if(_gameOver) return;
        if (!_roundActive) return;

        _roundElapsedTime += deltaTime;

        if (_player.state == BattleState.Casting && _ai.ctx.state == BattleState.Casting)
        {
            ResolveRound();
            return;
        }

        if (_roundElapsedTime >= ROUND_TIMEOUT)
        {
            if (_player.state != BattleState.Casting && _ai.ctx.state == BattleState.Casting)
            {
                ResolvePlayerFailed();
            }
            else if (_player.state == BattleState.Casting && _ai.ctx.state != BattleState.Casting)
            {
                ResolveAiFailed();
            }
            else
            {
                ResolveBothFailed();
            }
            return;
        }
    }

    public void RequestNextRound()
    {
        if (_gameOver) return;
        StartRound();
    }

    public void StartRound()
    {
        _roundActive = true;
        _roundElapsedTime = 0f;
        _ai.StartRound();
    }

    private void ResolveRound()
    {
        if (_gameOver) return;

        int pDmg = Calculate(_player.confirmedElement, _player.confirmedForm,
            _ai.ctx.confirmedElement, _ai.ctx.confirmedForm, _prevPlayerEl);
        int aiDmg = Calculate(_ai.ctx.confirmedElement, _ai.ctx.confirmedForm,
            _player.confirmedElement, _player.confirmedForm, _prevAiEl);

        HandPose playerWeak = GetWeakAgainst(_player.confirmedElement);
        if (_player.confirmedForm == HandPose.Special && _ai.ctx.confirmedElement == playerWeak)
        {
            pDmg += aiDmg;
            aiDmg = 0;
        }

        HandPose aiWeak = GetWeakAgainst(_ai.ctx.confirmedElement);
        if (_ai.ctx.confirmedForm == HandPose.Special && _player.confirmedElement == aiWeak)
        {
            aiDmg += pDmg;
            pDmg = 0;
        }

        int finalPDmg = 0;
        int finalAiDmg = 0;

        if (pDmg > aiDmg)
            finalPDmg = pDmg - aiDmg;
        else
            finalAiDmg = aiDmg - pDmg;

        _aiHp -= finalPDmg;
        _playerHp -= finalAiDmg;

        var result = new RoundResult
        {
            dmgDealtToAi = finalPDmg,
            dmgReceived = finalAiDmg
        };

        bool playerCounteredFlag = (_player.confirmedForm == HandPose.Special
                                    && _ai.ctx.confirmedElement == GetWeakAgainst(_player.confirmedElement));
        bool aiCounteredFlag = (_ai.ctx.confirmedForm == HandPose.Special
                                && _player.confirmedElement == GetWeakAgainst(_ai.ctx.confirmedElement));

        if (playerCounteredFlag || aiCounteredFlag)
            result.type = RoundResult.ResultType.Counter;
        else if (finalPDmg == 0 && finalAiDmg == 0)
            result.type = RoundResult.ResultType.Blocked;
        else
            result.type = RoundResult.ResultType.Normal;
        
        OnRoundResolved?.Invoke(result);
        
        Debug.Log("=== 라운드 결과 ===");
        Debug.Log($"Player: {_player.confirmedElement}+{_player.confirmedForm} (위력 {pDmg})");
        Debug.Log($"AI: {_ai.ctx.confirmedElement}+{_ai.ctx.confirmedForm} (위력 {aiDmg})");
        Debug.Log($"상쇄 후: Player가 AI에게 {finalPDmg}, AI가 Player에게 {finalAiDmg}");
        Debug.Log($"HP: Player {_playerHp} / AI {_aiHp}");

        // 같은 원소 연속 사용 시 -30% (직전 라운드와 비교, 1라운드 텀 두면 회복)
        if(_player.confirmedForm == HandPose.Attack)
            _prevPlayerEl = _player.confirmedElement;
        if(_ai.ctx.confirmedForm == HandPose.Attack)
            _prevAiEl = _ai.ctx.confirmedElement;

        if (_playerHp <= 0 || _aiHp <= 0)
        {
            _gameOver = true;
            Debug.Log(_playerHp <= 0 ? "패배..." : "승리!");
        }
        else
        {
            ResetCasters();
            _roundActive = false;
        }
    }

    private void ResolvePlayerFailed()
    {
        
        Debug.Log("=== 라운드 결과: Player 영창 실패 ===");

        int aiDmg = Calculate(_ai.ctx.confirmedElement, _ai.ctx.confirmedForm,
            HandPose.Unknown, HandPose.Unknown, _prevAiEl);
        
        Debug.Log($"AI: {_ai.ctx.confirmedElement}+{_ai.ctx.confirmedForm} (위력 {aiDmg})");
        Debug.Log($"Player 무방비 → {aiDmg} 받음");

        _playerHp -= aiDmg;
        
        Debug.Log($"HP: Player {_playerHp} / AI {_aiHp}");
        
        var result = new RoundResult
        {
            type = RoundResult.ResultType.Failed,
            dmgDealtToAi = 0,
            dmgReceived = aiDmg
        };
        OnRoundResolved?.Invoke(result);

        if (_ai.ctx.confirmedForm == HandPose.Attack)
            _prevAiEl = _ai.ctx.confirmedElement;

        if (_playerHp <= 0)
        {
            _gameOver = true;
            Debug.Log("패배...");
        }
        else
        {
            ResetCasters();
            _roundActive = false;
        }
    }
    
    private void ResolveAiFailed()
    {
        Debug.Log("=== 라운드 결과: AI 영창 실패 ===");

        int pDmg = Calculate(_player.confirmedElement, _player.confirmedForm,
            HandPose.Unknown, HandPose.Unknown, _prevPlayerEl);
    
        Debug.Log($"Player: {_player.confirmedElement}+{_player.confirmedForm} (위력 {pDmg})");
        Debug.Log($"AI 무방비 → {pDmg} 받음");

        _aiHp -= pDmg;
    
        var result = new RoundResult
        {
            type = RoundResult.ResultType.Failed,
            dmgDealtToAi = pDmg,
            dmgReceived = 0
        };
        OnRoundResolved?.Invoke(result);

        if (_player.confirmedForm == HandPose.Attack)
            _prevPlayerEl = _player.confirmedElement;

        if (_aiHp <= 0)
        {
            _gameOver = true;
        }
        else
        {
            ResetCasters();
            _roundActive = false;
        }
    }

    private void ResolveBothFailed()
    {
        Debug.Log("=== 라운드 결과: 둘 다 영창 실패 ===");
    
        var result = new RoundResult
        {
            type = RoundResult.ResultType.Blocked,
            dmgDealtToAi = 0,
            dmgReceived = 0
        };
        OnRoundResolved?.Invoke(result);

        if (_aiHp <= 0)
        {
            _gameOver = true;
        }
        else
        {
            ResetCasters();
            _roundActive = false;
        }
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
