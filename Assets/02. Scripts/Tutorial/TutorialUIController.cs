using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Constant;

public class TutorialUIController : MonoBehaviour
{
    [Header("Mission Info")]
    [SerializeField] private TextMeshProUGUI missionProgressText;   // "1 / 5"
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Image elementHandImage;
    [SerializeField] private Image formHandImage;

    [Header("Casting Status")]
    [SerializeField] private TextMeshProUGUI handStatusText;
    [SerializeField] private TextMeshProUGUI castingStatusText;

    [Header("Enemy Info")]
    [SerializeField] private TextMeshProUGUI enemyElementText;
    [SerializeField] private TextMeshProUGUI enemyStatusText;

    [Header("Success Toast")]
    [SerializeField] private GameObject successToast;
    [SerializeField] private CanvasGroup successCanvasGroup;

    [Header("Failure Toast")]
    [SerializeField] private GameObject failureToast;
    [SerializeField] private CanvasGroup failureCanvasGroup;

    [Header("Completion Panel")]
    [SerializeField] private GameObject completionPanel;

    private HandRecognizer _recognizer;
    private int _currentMissionIndex;
    private int _totalMissions;
    private Sequence _toastSequence;
    private Sequence _failureSequence;


    public void Init(TutorialManager tutorial, HandRecognizer recognizer, int totalMissions)
    {
        _recognizer = recognizer;
        _totalMissions = totalMissions;
        _currentMissionIndex = 0;

        // Bug2: 재진입 시 완료 패널 초기화 (이전 수련 완료 패널이 남아있는 경우 숨김)
        if (completionPanel != null) completionPanel.SetActive(false);

        tutorial.OnMissionChanged   += OnMissionChanged;
        tutorial.OnMissionSucceeded += OnMissionSucceeded;
        tutorial.OnMissionFailed    += OnMissionFailed;
        tutorial.OnTutorialCompleted += OnTutorialCompleted;
    }

    public void Cleanup(TutorialManager tutorial)
    {
        if (tutorial == null) return;
        tutorial.OnMissionChanged   -= OnMissionChanged;
        tutorial.OnMissionSucceeded -= OnMissionSucceeded;
        tutorial.OnMissionFailed    -= OnMissionFailed;
        tutorial.OnTutorialCompleted -= OnTutorialCompleted;
        _toastSequence?.Kill();
        _failureSequence?.Kill();
    }

    public void Refresh(CasterContext player, AICaster ai)
    {
        if (player == null) return;
        UpdateHandStatus();
        UpdateCastingStatus(player);
        if (ai != null) UpdateEnemyInfo(ai);
    }

    // ── 이벤트 핸들러 ───────────────────────────────────────────

    private void OnMissionChanged(TutorialMissionData data)
    {
        missionProgressText.text = $"{_currentMissionIndex + 1} / {_totalMissions}";
        instructionText.text = data.instructionText;

        bool hasElement = data.elementHandSprite != null;
        elementHandImage.gameObject.SetActive(hasElement);
        if (hasElement) elementHandImage.sprite = data.elementHandSprite;

        bool hasForm = data.formHandSprite != null;
        formHandImage.gameObject.SetActive(hasForm);
        if (hasForm) formHandImage.sprite = data.formHandSprite;
    }

    private void OnMissionSucceeded(TutorialMissionData data)
    {
        _currentMissionIndex++;
        SoundManager.Instance?.PlaySfx(Constant.SfxId.ElementConfirmed);
        ShowSuccessToast();
    }

    private void OnMissionFailed(TutorialMissionData data)
    {
        ShowFailureToast();
    }

    private void OnTutorialCompleted()
    {
        missionProgressText.text = $"{_totalMissions} / {_totalMissions}";
        if (completionPanel != null)
            completionPanel.SetActive(true);
    }

    // 완료 패널의 "메뉴로 돌아가기" 버튼에서 호출
    public void OnCompletionExitClicked()
    {
        GameManager.Instance.StopTutorial();
    }

    // ── 매 프레임 갱신 ─────────────────────────────────────────

    private void UpdateHandStatus()
    {
        if (handStatusText == null || _recognizer == null) return;

        bool left  = _recognizer.LeftStable;
        bool right = _recognizer.RightStable;

        handStatusText.color = new Color(1f, 0.78f, 0.34f); // 항상 노란색

        if (left && right)
            handStatusText.text = "양손 감지됨";
        else if (left || right)
            handStatusText.text = left ? "왼손만" : "오른손만";
        else
            handStatusText.text = "손이 안 보입니다";
    }

    private void UpdateCastingStatus(CasterContext player)
    {
        if (castingStatusText == null) return;

        Color purple = new Color(0.71f, 0.68f, 0.86f);
        Color gold   = new Color(1.00f, 0.78f, 0.34f);
        Color green  = new Color(0.114f, 0.62f, 0.459f);

        switch (player.state)
        {
            case BattleState.Idle:
                castingStatusText.text = "";
                break;

            case BattleState.ElementCharging:
                castingStatusText.text  = $"원소: {ElementKr(player.chargingElement)} 유지 중...";
                castingStatusText.color = purple;
                break;

            case BattleState.FormCharging:
                castingStatusText.text = player.chargingForm == HandPose.Unknown
                    ? $"[{ElementKr(player.confirmedElement)} 확정] 형태를 잡으세요"
                    : $"[{ElementKr(player.confirmedElement)}] 형태: {FormKr(player.chargingForm)} 유지 중...";
                castingStatusText.color = gold;
                break;

            case BattleState.Casting:
                castingStatusText.text  = $"{ElementKr(player.confirmedElement)} + {FormKr(player.confirmedForm)} 발동!";
                castingStatusText.color = green;
                break;

            default:
                castingStatusText.text = "";
                break;
        }
    }

    private void ShowSuccessToast()
    {
        _toastSequence?.Kill();
        successToast.SetActive(true);
        successCanvasGroup.alpha = 0f;

        _toastSequence = DOTween.Sequence()
            .Append(successCanvasGroup.DOFade(1f, 0.2f))
            .AppendInterval(0.8f)
            .Append(successCanvasGroup.DOFade(0f, 0.3f))
            .OnComplete(() => successToast.SetActive(false));
    }

    private void ShowFailureToast()
    {
        if (failureToast == null) return;
        _failureSequence?.Kill();
        failureToast.SetActive(true);
        failureCanvasGroup.alpha = 0f;

        _failureSequence = DOTween.Sequence()
            .Append(failureCanvasGroup.DOFade(1f, 0.2f))
            .AppendInterval(0.9f)
            .Append(failureCanvasGroup.DOFade(0f, 0.3f))
            .OnComplete(() => failureToast.SetActive(false));
    }

    private void UpdateEnemyInfo(AICaster ai)
    {
        if (enemyElementText == null) return;
        bool isEarly = ai.ctx.totalTime < 5f;
        if (ai.ctx.state == BattleState.Idle)
        {
            enemyElementText.text = "-";
            enemyStatusText.text  = "대기 중";
        }
        else if (ai.ctx.confirmedElement != HandPose.Unknown && isEarly)
        {
            enemyElementText.text = ElementKr(ai.ctx.confirmedElement);
            enemyStatusText.text  = "노출 중";
        }
        else
        {
            enemyElementText.text = "???";
            enemyStatusText.text  = "비밀";
        }
    }

    // ── 헬퍼 ───────────────────────────────────────────────────

    private string ElementKr(HandPose e) => e switch
    {
        HandPose.Fire  => "불",  HandPose.Water => "물",
        HandPose.Wind  => "바람", HandPose.Land  => "땅",
        _              => "?"
    };

    private string FormKr(HandPose f) => f switch
    {
        HandPose.Attack  => "공격", HandPose.Defense => "방어",
        HandPose.Special => "특수", _ => "?"
    };
}
