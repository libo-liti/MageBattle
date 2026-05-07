using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    private const int MAX_HP = 100;
    
    [Header("Player HP")]
    [SerializeField, Range(0, 100)] private int playerHp = 100;
    [SerializeField] private Image playerHpFill;       // Image 타입!
    [SerializeField] private TextMeshProUGUI playerHpText;
    
    [Header("AI HP")]
    [SerializeField, Range(0, 100)] private int aiHp = 100;
    [SerializeField] private Image aiHpFill;
    [SerializeField] private TextMeshProUGUI aiHpText;
    
    [Header("Casting Progress")]
    [SerializeField, Range(0, 1)] private float castingProgress = 0.3f;
    [SerializeField] private Image castingFill;

    private void Update()
    {
        UpdatePlayerHp(playerHp);
        UpdateAiHp(aiHp);
        UpdateCastingProgress(castingProgress);
    }

    public void UpdatePlayerHp(int hp)
    {
        float ratio = (float)hp / MAX_HP;
        playerHpFill.fillAmount = ratio;
        playerHpText.text = $"{hp} / {MAX_HP}";
    }

    public void UpdateAiHp(int hp)
    {
        float ratio = (float)hp / MAX_HP;
        aiHpFill.fillAmount = ratio;
        aiHpText.text = $"{hp} / {MAX_HP}";
    }

    public void UpdateCastingProgress(float progress01)
    {
        castingFill.fillAmount = progress01;
    }
}
