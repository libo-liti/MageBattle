using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Constant;

public class GameUIController : MonoBehaviour
{
    private const int MAX_HP = 100;
    
    [Header("Player HP")]
    [SerializeField] private Image playerHpFill;       // Image 타입!
    [SerializeField] private TextMeshProUGUI playerHpText;
    
    [Header("AI HP")]
    [SerializeField] private Image aiHpFill;
    [SerializeField] private TextMeshProUGUI aiHpText;
    
    [Header("Casting Progress")]
    [SerializeField] private Image castingFill;
    [SerializeField] private TextMeshProUGUI castingLabel;
    
    [Header("Webcam Hand Status")]
    [SerializeField] private TextMeshProUGUI handStatusText;
    [SerializeField] private TextMeshProUGUI poseInfoText;

    [Header("Enemy Magic Info")]
    [SerializeField] private TextMeshProUGUI enemyElementText;
    [SerializeField] private TextMeshProUGUI enemyStatusText;
    
    // 필드 영역 추가
    [Header("Round Result Toast")]
    [SerializeField] private GameObject toastObject;
    [SerializeField] private CanvasGroup toastCanvasGroup;
    [SerializeField] private Image toastIcon;
    [SerializeField] private TextMeshProUGUI toastSymbol;
    [SerializeField] private TextMeshProUGUI toastLabel;
    [SerializeField] private TextMeshProUGUI toastValue;
    [SerializeField] private Outline toastOutline;

    [Header("Persona Toast (라이벌 도발)")]
    [SerializeField] private GameObject personaToastObject;
    [SerializeField] private CanvasGroup personaToastCanvasGroup;
    [SerializeField] private TextMeshProUGUI personaToastText;

    private Sequence _currentToastSequence;
    private Sequence _currentPersonaSequence;

    private void OnEnable()
    {
        if (PersonaManager.Instance != null)
            PersonaManager.Instance.OnTauntFired += ShowPersonaToast;
    }

    private void OnDisable()
    {
        if (PersonaManager.Instance != null)
            PersonaManager.Instance.OnTauntFired -= ShowPersonaToast;
    }

    public void ShowPersonaToast(string message)
    {
        if (personaToastObject == null || personaToastCanvasGroup == null || personaToastText == null)
            return;

        _currentPersonaSequence?.Kill();
        personaToastObject.SetActive(true);
        personaToastText.text = message;
        personaToastCanvasGroup.alpha = 0f;

        _currentPersonaSequence = DOTween.Sequence()
            .Append(personaToastCanvasGroup.DOFade(1f, 0.25f))
            .AppendInterval(2.5f)
            .Append(personaToastCanvasGroup.DOFade(0f, 0.4f))
            .OnComplete(() =>
            {
                personaToastObject.SetActive(false);
                _currentPersonaSequence = null;
            });
    }

    public void HidePersonaToast()
    {
        _currentPersonaSequence?.Kill();
        _currentPersonaSequence = null;
        if (personaToastCanvasGroup != null) personaToastCanvasGroup.alpha = 0f;
        if (personaToastObject     != null) personaToastObject.SetActive(false);
    }
    
    public void Refresh(BattleManager battle, HandRecognizer recognizer)
    {
        if (battle == null) return;
        
        // HP 갱신
        UpdatePlayerHp(battle.RoundManager.PlayerHp);
        UpdateAiHp(battle.RoundManager.AiHp);
        
        // 영창 진행
        // float progress = battle.RoundManager.RoundElapsedTime / RoundManager.ROUND_TIMEOUT;
        UpdateCastingProgress(battle);
        
        // 적 영창 정보
        UpdateEnemyMagicInfo(battle.AI);
        
        // 손 감지 상태 (NEW)
        UpdateHandStatus(recognizer, battle.Player);
    }
    
    private void UpdateHandStatus(HandRecognizer recognizer, CasterContext player)
    {
        Color green  = new Color(0.114f, 0.62f, 0.459f);
        Color orange = new Color(1.0f, 0.42f, 0.21f);
        Color red    = new Color(0.886f, 0.294f, 0.290f);
    
        bool left  = recognizer.LeftStable;
        bool right = recognizer.RightStable;
    
        // 메인 상태
        if (left && right)
        {
            handStatusText.text = "양손 감지됨";
            handStatusText.color = green;
        }
        else if (left)
        {
            handStatusText.text = "왼손만";
            handStatusText.color = orange;
        }
        else if (right)
        {
            handStatusText.text = "오른손만";
            handStatusText.color = orange;
        }
        else
        {
            handStatusText.text = "손이 안 보입니다";
            handStatusText.color = red;
        }
    
        // PoseInfoText 비활성 또는 빈 텍스트 — 게임 단계 안내가 메인이라 중복 X
        if (poseInfoText != null)
        {
            poseInfoText.text = "";
        }
    }

    private void UpdatePlayerHp(int hp)
    {
        playerHpFill.fillAmount = (float)hp / MAX_HP;
        playerHpText.text = $"{hp} / {MAX_HP}";
    }

    private void UpdateAiHp(int hp)
    {
        aiHpFill.fillAmount = (float)hp / MAX_HP;
        aiHpText.text = $"{hp} / {MAX_HP}";
    }

    private void UpdateCastingProgress(BattleManager battle)
    {
        var player = battle.Player;
        float progress = battle.RoundManager.RoundElapsedTime / RoundManager.ROUND_TIMEOUT;
    
        // 진행 바 fill
        castingFill.fillAmount = Mathf.Clamp01(progress);
    
        // 진행 바 색 (시간 위급도)
        if (progress < 0.5f)
            castingFill.color = new Color(0.71f, 0.68f, 0.86f);  // 보라 (여유)
        else
            castingFill.color = new Color(1.0f, 0.42f, 0.21f);   // 주황 (위급)
    
        // 라벨 텍스트 + 색 (게임 단계)
        string text;
        Color color;
    
        Color purple = new Color(0.71f, 0.68f, 0.86f);  // 연보라 (대기·진행)
        Color gold   = new Color(1.0f, 0.78f, 0.34f);   // 금색 (확정·다음 단계)
        Color green  = new Color(0.114f, 0.62f, 0.459f); // 녹색 (발동 완료)
    
        switch (player.state)
        {
            case BattleState.Idle:
                text = "원소를 선택하세요";
                color = purple;
                break;
        
            case BattleState.ElementCharging:
                text = $"원소: {GetElementKr(player.chargingElement)} 유지 중...";
                color = purple;
                break;
        
            case BattleState.FormCharging:
                if (player.chargingForm == HandPose.Unknown)
                {
                    text = $"[{GetElementKr(player.confirmedElement)} 확정] 형태를 잡으세요";
                    color = gold;
                }
                else
                {
                    text = $"[{GetElementKr(player.confirmedElement)}] 형태: {GetFormKr(player.chargingForm)} 유지 중...";
                    color = gold;
                }
                break;
        
            case BattleState.Casting:
                text = $"{GetElementKr(player.confirmedElement)} + {GetFormKr(player.confirmedForm)} 발동!";
                color = green;
                break;
        
            default:
                text = "";
                color = purple;
                break;
        }
    
        castingLabel.text = text;
        castingLabel.color = color;
    }
    
    // 헬퍼 메서드 — 한국어 변환
    private string GetElementKr(HandPose element)
    {
        switch (element)
        {
            case HandPose.Fire:  return "불";
            case HandPose.Water: return "물";
            case HandPose.Wind:  return "바람";
            case HandPose.Land:  return "땅";
            default:             return "?";
        }
    }

    private string GetFormKr(HandPose form)
    {
        switch (form)
        {
            case HandPose.Attack:  return "공격";
            case HandPose.Defense: return "방어";
            case HandPose.Special: return "특수";
            default:               return "?";
        }
    }

    private void UpdateEnemyMagicInfo(AICaster ai)
    {
        bool isEarly = ai.ctx.totalTime < 5f;

        if (ai.ctx.state == BattleState.Idle)
        {
            enemyElementText.text = "-";
            enemyStatusText.text = "대기 중";
        }
        else if (ai.ctx.confirmedElement != HandPose.Unknown && isEarly)
        {
            enemyElementText.text = ai.ctx.confirmedElement.ToString().ToUpper();
            enemyStatusText.text = "노출 중";
        }
        else
        {
            enemyElementText.text = "???";
            enemyStatusText.text = "비밀";
        }
    }

    public void ShowToast(RoundResult result)
    {
        _currentToastSequence?.Kill();

        SetupToastVisual(result);
        PlayToastAnimation();
    }

    // 게임 시작 시 이전 게임의 토스트 잔류 제거
    public void HideToast()
    {
        _currentToastSequence?.Kill();
        _currentToastSequence = null;
        if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;
        if (toastObject     != null) toastObject.SetActive(false);
        HidePersonaToast();
    }

    private void SetupToastVisual(RoundResult result)
    {
        Color green  = new Color(0.114f, 0.62f, 0.459f);
        Color red    = new Color(0.886f, 0.294f, 0.290f);
        Color gold   = new Color(1.0f, 0.784f, 0.341f);
        Color gray   = new Color(0.227f, 0.212f, 0.329f);
        Color dark   = new Color(0.055f, 0.078f, 0.122f);
        
        switch (result.type)
        {
            case RoundResult.ResultType.Counter:
                int counterDmg = Mathf.Abs(result.dmgDealtToAi - result.dmgReceived);

                if (counterDmg == 0)
                {
                    toastIcon.color = gray;
                    toastOutline.effectColor = gray;
                    toastSymbol.text = "★";
                    toastSymbol.color = new Color(0.71f, 0.68f, 0.86f);
                    toastLabel.text = "카운터 무효";
                    toastValue.text = "반사 없음";
                    toastValue.color = new Color(0.71f, 0.68f, 0.86f);
                }
                else if (result.dmgDealtToAi > result.dmgReceived)
                {
                    // Player가 카운터 성공 (반사)
                    toastIcon.color = gold;
                    toastOutline.effectColor = gold;
                    toastSymbol.text = "★";
                    toastSymbol.color = dark;
                    toastLabel.text = "SPECIAL 카운터!";
                    toastValue.text = $"+{result.dmgDealtToAi} 반사";
                    toastValue.color = green;
                }
                else
                {
                    // Player가 카운터 당함
                    toastIcon.color = red;
                    toastOutline.effectColor = red;
                    toastSymbol.text = "★";
                    toastSymbol.color = dark;
                    toastLabel.text = "카운터 당함";
                    toastValue.text = $"-{result.dmgReceived}";
                    toastValue.color = red;
                }
                break;
                
            case RoundResult.ResultType.Blocked:
                toastIcon.color = gray;
                toastOutline.effectColor = gray;
                toastSymbol.text = "≡";
                toastSymbol.color = new Color(0.71f, 0.68f, 0.86f);
                toastLabel.text = "상쇄";
                toastValue.text = "둘 다 막힘";
                toastValue.color = new Color(0.71f, 0.68f, 0.86f);
                break;
            
            case RoundResult.ResultType.Failed:
                toastIcon.color = red;
                toastOutline.effectColor = red;
                toastSymbol.text = "!";
                toastSymbol.color = dark;
                toastLabel.text = "무방비";
                toastValue.text = $"-{result.dmgReceived}";
                toastValue.color = red;
                break;
                
            case RoundResult.ResultType.Normal:
                int net = Mathf.Abs(result.dmgDealtToAi - result.dmgReceived);
                if (result.dmgDealtToAi > result.dmgReceived)
                {
                    toastIcon.color = green;
                    toastSymbol.text = "↑";
                    toastLabel.text = "데미지 적중";
                    toastValue.text = $"+{net}";
                    toastValue.color = green;
                }
                else
                {
                    toastIcon.color = red;
                    toastSymbol.text = "↓";
                    toastLabel.text = "데미지 받음";
                    toastValue.text = $"-{net}";
                    toastValue.color = red;
                }
                break;
        }
    }

    private void PlayToastAnimation()
    {
        toastObject.SetActive(true);

        RectTransform rt = toastObject.GetComponent<RectTransform>();

        rt.anchoredPosition = new Vector2(0, -80);
        toastCanvasGroup.alpha = 0;

        _currentToastSequence = DOTween.Sequence()
            .Append(rt.DOAnchorPosY(-120, 0.25f).SetEase(Ease.OutBack))
            .Join(toastCanvasGroup.DOFade(1f, 0.2f))
            .AppendInterval(1.5f)
            .Append(toastCanvasGroup.DOFade(0f, 0.3f))
            .Join(rt.DOAnchorPosY(-80, 0.3f))
            .OnComplete(() =>
            {
                toastObject.SetActive(false);
                _currentToastSequence = null;
            });
    }
}
