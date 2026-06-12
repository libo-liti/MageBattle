using UnityEngine;

[CreateAssetMenu(fileName = "RivalData", menuName = "Mage Battle/Rival Data")]
public class RivalData : ScriptableObject
{
    [Header("기본 정보")]
    public string rivalId;
    public string displayName;
    [Range(1, 3)]
    public int difficulty;
    [TextArea(2, 4)]
    public string description;

    [Header("AI 설정")]
    public float aiTimeMin = 4f;
    public float aiTimeMax = 7f;

    [Header("AI 형태 선호도 (가중치, 합이 1일 필요 없음)")]
    [Min(0f)] public float attackWeight = 1f;
    [Min(0f)] public float defenseWeight = 1f;
    [Min(0f)] public float specialWeight = 1f;
    
    [Header("LLM 페르소나 (Day 3~4)")]
    [TextArea(5, 12)]
    public string personaPrompt;          
    
    [Header("도발 메시지 (JSON 파일 경로)")]
    public string tauntJsonFileName;      
    
    [Header("TTS (Day 5)")]
    public string ttsVoiceName;           
    public float ttsPitch = 1.0f;
    public float ttsSpeed = 1.0f;
    
    [Header("UI (4주차)")]
    public Sprite portrait;
    
    [Header("잠금 해제")]
    public string requiredRivalId;
}
