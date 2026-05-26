// Assets/02. Scripts/RivalCard.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RivalCard : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private RivalData rivalData;
    
    [Header("UI Refs")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button challengeButton;
    [SerializeField] private TextMeshProUGUI challengeButtonText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Outline cardOutline;
    
    private void OnEnable()
    {
        ApplyData();
    }
    
    private void ApplyData()
    {
        if (rivalData == null) return;
        
        // 데이터 적용
        nameText.text = rivalData.displayName;
        difficultyText.text = GetDifficultyStars(rivalData.difficulty);
        descriptionText.text = rivalData.description;
        
        if (portraitImage != null && rivalData.portrait != null)
            portraitImage.sprite = rivalData.portrait;

        // 잠금 상태 결정
        bool unlocked = IsUnlocked();
        SetLockState(unlocked);
    }
    
    private string GetDifficultyStars(int level)
    {
        switch (level)
        {
            case 1: return "★ ☆ ☆";
            case 2: return "★ ★ ☆";
            case 3: return "★ ★ ★";
            default: return "";
        }
    }
    
    private bool IsUnlocked()
    {
        if (string.IsNullOrEmpty(rivalData.requiredRivalId))
            return true;
        return SaveSystem.IsRivalDefeated(rivalData.requiredRivalId);
    }
    
    private void SetLockState(bool unlocked)
    {
        if (unlocked)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            challengeButton.interactable = true;
            challengeButtonText.text = "도전";
            challengeButtonText.color = new Color(0.102f, 0.078f, 0.196f); 
            cardOutline.effectColor = new Color(1.0f, 0.784f, 0.341f); 
        
            // 버튼 색 변경 (Image 컴포넌트 직접)
            var btnImage = challengeButton.GetComponent<Image>();
            if (btnImage != null)
                btnImage.color = new Color(1.0f, 0.784f, 0.341f); 
        }
        else
        {
            canvasGroup.alpha = 0.5f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            challengeButton.interactable = false;
            challengeButtonText.text = "잠김";
            challengeButtonText.color = new Color(0.4f, 0.4f, 0.4f); 
            cardOutline.effectColor = new Color(0.227f, 0.212f, 0.329f); 
        
            var btnImage = challengeButton.GetComponent<Image>();
            if (btnImage != null)
                btnImage.color = new Color(0.165f, 0.125f, 0.278f); 
        }
    }
    
    // Inspector onClick에 연결
    public void OnChallengeClicked()
    {
        SoundManager.Instance?.PlaySfx(Constant.SfxId.UiClick);
        GameManager.Instance.StartGameWithRival(rivalData);
    }
}