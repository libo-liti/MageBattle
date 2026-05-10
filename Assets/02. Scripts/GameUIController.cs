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
    [SerializeField] private UnityEngine.UI.Outline toastOutline;

    private Sequence _currentToastSequence;
    
    public void Refresh(BattleManager battle)
    {
        if (battle == null) return;
        
        // HP 갱신
        UpdatePlayerHp(battle.RoundManager.PlayerHp);
        UpdateAiHp(battle.RoundManager.AiHp);
        
        // 영창 진행
        float progress = battle.RoundManager.RoundElapsedTime / RoundManager.ROUND_TIMEOUT;
        UpdateCastingProgress(progress);
        
        // 적 영창 정보
        UpdateEnemyMagicInfo(battle.AI);
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

    private void UpdateCastingProgress(float progress01)
    {
        castingFill.fillAmount = Mathf.Clamp01(progress01);

        if (progress01 < 0.5f)
            castingFill.color = new Color(0.71f, 0.68f, 0.86f);  // 보라 (여유)
        else
            castingFill.color = new Color(1.0f, 0.42f, 0.208f);  // 주황 (위급)
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
