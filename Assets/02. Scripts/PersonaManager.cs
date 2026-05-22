using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 라이벌 도발 메시지 관리.
/// 현재는 JSON 폴백 대사만 사용. 추후 LLM for Unity 통합 시 LLM 호출 → 실패 시 폴백 순서로 동작.
/// </summary>
public class PersonaManager : MonoBehaviour
{
    public static PersonaManager Instance { get; private set; }

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
    /// 트리거 발동 — 현재 라이벌의 대사를 무작위로 선택해 OnTauntFired 이벤트로 전파.
    /// 같은 트리거가 한 게임에 두 번 발동되면 무시.
    /// </summary>
    public void TriggerPersona(string triggerId)
    {
        if (_currentRival == null || string.IsNullOrEmpty(triggerId)) return;
        if (_firedTriggersThisGame.Contains(triggerId)) return;
        _firedTriggersThisGame.Add(triggerId);

        string message = GetFallbackTaunt(_currentRival, triggerId);
        if (string.IsNullOrEmpty(message)) return;

        OnTauntFired?.Invoke(message);
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

    // ── JSON 데이터 구조 (JsonUtility는 Dictionary 직접 못 다룸) ─────────

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
