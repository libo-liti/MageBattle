using UnityEngine;
using static Constant;

public enum TutorialMissionType
{
    None,
    LearnElement,
    LearnFullCast,
    LearnElementSwap,
    LearnDefense,
    LearnCounter,
    Completed,
}

[CreateAssetMenu(fileName = "TutorialMission", menuName = "Game/Tutorial/Mission")]
public class TutorialMissionData : ScriptableObject
{
    [Header("Mission Info")]
    public TutorialMissionType type;
    
    [TextArea(2, 4)]
    public string instructionText;
    
    [Header("Player Requirement")]
    [Tooltip("플레이어가 해야 할 원소 (Unknown이면 아무거나)")]
    public HandPose requiredElement;
    
    [Tooltip("플레이어가 해야 할 형태 (Unknown이면 아무거나)")]
    public HandPose requiredForm;
    
    [Header("AI Behavior")]
    [Tooltip("AI 원소 (Unknown이면 AI가 가만히 있음)")]
    public HandPose aiElement;
    
    public HandPose aiForm;
    public float aiDuration = 6f;
    
    [Header("UI Visuals")]
    [Tooltip("자막 옆에 표시할 원소 손 모양 (없으면 비활성)")]
    public Sprite elementHandSprite;
    
    [Tooltip("자막 옆에 표시할 형태 손 모양 (없으면 비활성)")]
    public Sprite formHandSprite;
}