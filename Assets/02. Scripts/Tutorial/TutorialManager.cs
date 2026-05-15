using System;
using UnityEngine;
using static Constant;

public class TutorialManager : MonoBehaviour
{
    public event Action<TutorialMissionData> OnMissionChanged;      // 미션 시작/전환 시 호출
    public event Action<TutorialMissionData> OnMissionSucceeded;    // 미션 성공 시 호출 (피드백용)
    public event Action OnTutorialCompleted;                    // 전체 튜토리얼 완료
    
    private TutorialMissionData[] _missions;
    private int _currentIndex = -1;
    
    private CasterContext _player;
    private AICaster _ai;
    
    // 현재 미션
    public TutorialMissionData CurrentMissionData => 
        (_currentIndex >= 0 && _currentIndex < _missions.Length) 
            ? _missions[_currentIndex] 
            : null;
    
    public bool IsCompleted => _currentIndex >= _missions.Length;

    public TutorialManager(CasterContext player, AICaster ai, TutorialConfig config)
    {
        _player = player;
        _ai = ai;
        _missions = config.missions;
    }
    
    /// <summary>
    /// 튜토리얼 시작 (첫 미션부터)
    /// </summary>
    public void Start()
    {
        _currentIndex = 0;
        StartCurrentMission();
    }

    /// <summary>
    /// 현재 미션을 실제로 시작 (AI 행동 설정 + UI 이벤트 발행)
    /// </summary>
    private void StartCurrentMission()
    {
        var mission = CurrentMissionData;
        if (mission == null) return;

        // AI 행동 설정
        if (mission.aiElement == HandPose.Unknown)
        {
            // AI 가만히
            _ai.StartIdleRound();
        }
        else
        {
            // AI가 특정 원소+형태로 영창
            _ai.StartRound(mission.aiElement, mission.aiForm, mission.aiDuration);
        }

        // UI에 알리기
        OnMissionChanged?.Invoke(mission);
    }

    /// <summary>
    /// 매 프레임 호출 - 현재 미션 성공 여부 체크
    /// </summary>
    public void Update()
    {
        if (CurrentMissionData == null) return;

        if (CheckMissionSuccess(CurrentMissionData))
        {
            OnMissionSucceeded?.Invoke(CurrentMissionData);
            AdvanceToNextMission();
        }
    }

    /// <summary>
    /// 미션 성공 조건 체크
    /// </summary>
    private bool CheckMissionSuccess(TutorialMissionData missionData)
    {
        // 미션 1: 원소만 학습 - Player가 ElementCharging에서 FormCharging으로 전환되면 성공
        // 즉, 원소가 confirmed되었으면 성공
        if (missionData.type == TutorialMissionType.LearnElement)
        {
            return _player.confirmedElement == missionData.requiredElement;
        }

        // 미션 2~5: 전체 영창 완료 (Casting 상태 도달)
        if (_player.state != BattleState.Casting) return false;

        // 원소 일치 체크 (Unknown이면 아무거나 OK)
        bool elementOk = missionData.requiredElement == HandPose.Unknown 
                         || _player.confirmedElement == missionData.requiredElement;
        
        // 형태 일치 체크
        bool formOk = missionData.requiredForm == HandPose.Unknown
                      || _player.confirmedForm == missionData.requiredForm;

        return elementOk && formOk;
    }

    /// <summary>
    /// 다음 미션으로 전환
    /// </summary>
    private void AdvanceToNextMission()
    {
        _currentIndex++;
        
        // 플레이어 상태 리셋 (다음 미션 위해)
        ResetPlayer();
        
        if (_currentIndex >= _missions.Length)
        {
            // 모든 미션 완료
            OnTutorialCompleted?.Invoke();
            return;
        }

        StartCurrentMission();
    }

    private void ResetPlayer()
    {
        _player.state = BattleState.Idle;
        _player.confirmedElement = HandPose.Unknown;
        _player.confirmedForm = HandPose.Unknown;
        _player.chargingElement = HandPose.Unknown;
        _player.chargingForm = HandPose.Unknown;
        _player.holdTime = 0f;
        _player.totalTime = 0f;
        
        _ai.Reset();
    }
}
