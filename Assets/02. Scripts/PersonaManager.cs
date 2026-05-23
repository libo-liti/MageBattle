using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;

/// <summary>
/// 라이벌 도발 메시지 관리.
/// LLM(LLM for Unity) 호출 → 실패/타임아웃 시 JSON 폴백 대사로 자동 전환.
/// </summary>
public class PersonaManager : MonoBehaviour
{
    public static PersonaManager Instance { get; private set; }

    [Header("LLM Settings")]
    [SerializeField] private LLMCharacter llmCharacter;
    [SerializeField] private float llmTimeoutSec = 10f;   // 첫 추론은 느릴 수 있음 (기본 10초)
    [SerializeField] private int maxResponseLength = 50;

    // 라이벌 → 트리거 → 대사 리스트
    private readonly Dictionary<string, TauntData> _tauntsByRival = new Dictionary<string, TauntData>();
    private readonly System.Random _rng = new System.Random();
    private RivalData _currentRival;

    // 트리거 ID 상수 (오타 방지)
    public const string TRIG_GAME_START          = "game_start";
    public const string TRIG_AI_COUNTER_SUCCESS  = "ai_counter_success";
    public const string TRIG_AI_VICTORY          = "ai_victory";
    public const string TRIG_AI_DEFEAT           = "ai_defeat";
    public const string TRIG_PLAYER_SAME_ELEMENT = "player_same_element";
    public const string TRIG_PLAYER_CAST_FAIL    = "player_cast_fail";
    public const string TRIG_PLAYER_LOW_HP       = "player_low_hp";

    // 한 게임당 트리거가 이미 발동됐는지 추적 (중복 도발 방지)
    private readonly HashSet<string> _firedTriggersThisGame = new HashSet<string>();

    public event Action<string> OnTauntFired; // UI 토스트에 연결

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 게임 시작 시 호출 — 라이벌 설정 + 트리거 발동 기록 리셋.
    /// </summary>
    public void SetCurrentRival(RivalData rival)
    {
        _currentRival = rival;
        _firedTriggersThisGame.Clear();
    }

    /// <summary>
    /// 트리거 발동 — LLM 호출 → 실패 시 JSON 폴백.
    /// 같은 트리거가 한 게임에 두 번 발동되면 무시.
    /// </summary>
    public async void TriggerPersona(string triggerId)
    {
        if (_currentRival == null || string.IsNullOrEmpty(triggerId)) return;
        if (_firedTriggersThisGame.Contains(triggerId)) return;
        _firedTriggersThisGame.Add(triggerId);

        // game_start는 LLM 서버 콜드스타트 대기 (첫 추론 전 3초 여유)
        if (triggerId == TRIG_GAME_START)
            await Task.Delay(3000);

        // 1) LLM 호출 시도
        string message = await TryGetLLMResponse(triggerId);
        bool fromLLM = !string.IsNullOrEmpty(message);

        // 2) LLM 실패 → 폴백
        if (!fromLLM)
            message = GetFallbackTaunt(_currentRival, triggerId);

        if (string.IsNullOrEmpty(message)) return;

        OnTauntFired?.Invoke(message);
    }

    /// <summary>
    /// LLM 비동기 호출 — 타임아웃 초과 또는 실패 시 null 반환 (폴백으로 전환됨).
    /// </summary>
    private async Task<string> TryGetLLMResponse(string triggerId)
    {
        if (llmCharacter == null) return null;
        if (_currentRival == null || string.IsNullOrEmpty(_currentRival.personaPrompt)) return null;

        try
        {
            llmCharacter.SetPrompt(BuildSystemPrompt(triggerId), clearChat: true);

            var chatTask = llmCharacter.Chat(GetTriggerQuery(triggerId), null, null, addToHistory: false);
            var timeoutTask = Task.Delay((int)(llmTimeoutSec * 1000));

            var finished = await Task.WhenAny(chatTask, timeoutTask);
            if (finished == chatTask)
                return CleanResponse(await chatTask);

            llmCharacter.CancelRequests();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private string BuildSystemPrompt(string triggerId)
    {
        return _currentRival.personaPrompt
             + "\n\n[응답 형식 규칙]\n"
             + "- 한국어로 1-2문장만, 최대 50자\n"
             + "- 따옴표·인용·해설·이모티콘 없이 대사만 출력\n"
             + "- 현재 상황: " + GetTriggerDescription(triggerId);
    }

    private string GetTriggerQuery(string triggerId)
    {
        return "지금 한마디 해줘.";
    }

    private string GetTriggerDescription(string triggerId)
    {
        switch (triggerId)
        {
            case TRIG_GAME_START:          return "도전자가 결투를 신청해 마주섰다";
            case TRIG_AI_COUNTER_SUCCESS:  return "내가 카운터(반사)로 상대 마법을 되돌렸다";
            case TRIG_AI_VICTORY:          return "내가 상대를 쓰러뜨리고 승리했다";
            case TRIG_AI_DEFEAT:           return "내가 상대에게 패배했다";
            case TRIG_PLAYER_SAME_ELEMENT: return "상대가 같은 원소를 또 사용해서 약해진 공격을 했다";
            case TRIG_PLAYER_CAST_FAIL:    return "상대가 영창에 실패해 무방비가 됐다";
            case TRIG_PLAYER_LOW_HP:       return "상대의 HP가 30% 이하로 위태롭다";
            default:                        return "결투 중";
        }
    }

    private string CleanResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;

        response = response.Replace("\n", " ").Replace("\r", " ")
                           .Replace("\"", "").Replace("'", "")
                           .Trim();

        if (response.Length == 0) return null;
        if (response.Length > maxResponseLength)
            response = response.Substring(0, maxResponseLength).TrimEnd() + "…";

        return response;
    }

    /// <summary>
    /// 폴백 대사 가져오기 (JSON에서 무작위 1개).
    /// </summary>
    private string GetFallbackTaunt(RivalData rival, string triggerId)
    {
        if (!_tauntsByRival.TryGetValue(rival.rivalId, out var data))
        {
            data = LoadTauntJson(rival.tauntJsonFileName);
            if (data == null) return null;
            _tauntsByRival[rival.rivalId] = data;
        }

        if (data.taunts == null) return null;
        if (!data.taunts.TryGetValue(triggerId, out var lines)) return null;
        if (lines == null || lines.Count == 0) return null;

        return lines[_rng.Next(lines.Count)];
    }

    private TauntData LoadTauntJson(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        string path = Path.Combine(Application.streamingAssetsPath, "Taunts", fileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[PersonaManager] Taunt 파일을 찾을 수 없습니다: {path}");
            return null;
        }
        try
        {
            string json = File.ReadAllText(path);
            var raw = JsonUtility.FromJson<TauntJsonRaw>(json);
            return TauntData.FromRaw(raw);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PersonaManager] JSON 파싱 실패 ({fileName}): {e.Message}");
            return null;
        }
    }

    // ── JSON 데이터 구조 ─────────────────────────────────────────────────

    private class TauntData
    {
        public string rivalId;
        public Dictionary<string, List<string>> taunts;

        public static TauntData FromRaw(TauntJsonRaw raw)
        {
            if (raw == null || raw.taunts == null) return null;
            var d = new TauntData { rivalId = raw.rivalId };
            d.taunts = new Dictionary<string, List<string>>
            {
                { TRIG_GAME_START,          ToList(raw.taunts.game_start) },
                { TRIG_AI_COUNTER_SUCCESS,  ToList(raw.taunts.ai_counter_success) },
                { TRIG_AI_VICTORY,          ToList(raw.taunts.ai_victory) },
                { TRIG_AI_DEFEAT,           ToList(raw.taunts.ai_defeat) },
                { TRIG_PLAYER_SAME_ELEMENT, ToList(raw.taunts.player_same_element) },
                { TRIG_PLAYER_CAST_FAIL,    ToList(raw.taunts.player_cast_fail) },
                { TRIG_PLAYER_LOW_HP,       ToList(raw.taunts.player_low_hp) },
            };
            return d;
        }

        private static List<string> ToList(string[] arr) =>
            arr == null ? new List<string>() : new List<string>(arr);
    }

    [Serializable]
    private class TauntJsonRaw
    {
        public string rivalId;
        public TauntsBlock taunts;
    }

    [Serializable]
    private class TauntsBlock
    {
        public string[] game_start;
        public string[] ai_counter_success;
        public string[] ai_victory;
        public string[] ai_defeat;
        public string[] player_same_element;
        public string[] player_cast_fail;
        public string[] player_low_hp;
    }
}
