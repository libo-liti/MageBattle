using System;
using Pose.DetailedVisualizer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Constant;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private RivalData _currentRival;
    
    [Header("Hand Tracking")]
    [SerializeField] private HandVisualizer hand;
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject dojoSelectPanel;

    [Header("Result UI")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI subText;
    [SerializeField] private TextMeshProUGUI statValueText;
    [SerializeField] private Button nextButton;

    [Header("UI Controller")]
    [SerializeField] private GameUIController uiController;
    
    private bool _showDebug = true;
    
    private HandRecognizer _recognizer;
    private BattleManager _battle;
    private GameState _state = GameState.MainMenu;
    
    private void Awake()
    {
        Instance = this;
        
        _recognizer = new HandRecognizer(hand, leftHand, rightHand);
        _battle = new BattleManager();
        SubscribeToBattle();
    }

    private void Start()
    {
        ShowMainMenu();
    }

    private void Update()
    {
        if (_state == GameState.Playing)
        {
            _recognizer.Update();
            _battle.Update(_recognizer.CurrentPose, Time.deltaTime);
            
            uiController.Refresh(_battle);
            
            if(_battle.RoundManager.GameOver)
                ShowGameOver();
        }
        
        if (Input.GetKeyDown(KeyCode.F1))
            _showDebug = !_showDebug;
        if(Input.GetKeyDown(KeyCode.F2))
            SaveSystem.UnlockAll();
        if(Input.GetKeyDown(KeyCode.F3))
            SaveSystem.ResetAll();
    }

    public void StartGameWithRival(RivalData rival)
    {
        _currentRival = rival;
        StartGame();
    }

    public void ShowMainMenu()
    {
        _state = GameState.MainMenu;
        mainMenuPanel.SetActive(true);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        dojoSelectPanel.SetActive(false);
    }
    
    public void ShowDojoSelect()
    {
        _state = GameState.MainMenu;
        mainMenuPanel.SetActive(false);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        dojoSelectPanel.SetActive(true);
    }

    public void StartGame()
    {
        _state = GameState.Playing;
        mainMenuPanel.SetActive(false);
        gamePanel.SetActive(true);
        gameOverPanel.SetActive(false);
        dojoSelectPanel.SetActive(false);

        UnsubscribeFromBattle();
        _battle = new BattleManager(_currentRival);
        SubscribeToBattle();
    }

    public void ShowGameOver()
    {
        _state = GameState.GameOver;
        gameOverPanel.SetActive(true);

        bool playerWon = _battle.RoundManager.AiHp <= 0;
        resultText.text = playerWon ? "승리!" : "패배...";

        if (_currentRival != null)
        {
            subText.text = playerWon 
                ? $"{_currentRival.displayName}을(를) 격파했습니다" 
                : "다음에 다시 도전해보세요";
            
            if(playerWon)
                SaveSystem.MarkRivalDefeated(_currentRival.rivalId);
        }
        statValueText.text = $"{_battle.RoundManager.PlayerHp} / 100";
        SaveSystem.RecordGameResult(playerWon);
        
        if(nextButton != null)
            nextButton.gameObject.SetActive(playerWon);
    }

    private void SubscribeToBattle()
    {
        _battle.RoundManager.OnRoundResolved += uiController.ShowToast;
    }

    private void UnsubscribeFromBattle()
    {
        if (_battle != null)
            _battle.RoundManager.OnRoundResolved -= uiController.ShowToast;
    }

    private void OnDestroy()
    {
        UnsubscribeFromBattle();
    }

    public void OnDojoBreakClicked()    // 메인 메뉴: 도장 깨기
    {
        ShowDojoSelect();
    }
    
    public void OnDojoBackClicked()
    {
        ShowMainMenu();
    }
    
    public void OnExitClicked()         // 메인 메뉴: 종료
    {
        Application.Quit();
    }
    
    public void OnNextClicked()         // 결과: 다음 단계 (지금은 메뉴로)
    {
        ShowMainMenu();
    }
    
    public void OnMenuClicked()         // 결과: 메뉴로
    {
        ShowMainMenu();
    }
    
    private void OnGUI()
    {
        if(_showDebug)
            DrawFullDebug();
        else
            DrawMinimalUI();
    }
    
    private void DrawMinimalUI()
    {
        GUIStyle bigStyle = new GUIStyle(GUI.skin.label);
        bigStyle.fontSize = 24;
        
        GUI.Label(new Rect(20, 20, 200, 40), $"HP : {_battle.RoundManager.PlayerHp}", bigStyle);
        
        GUI.Label(new Rect(Screen.width - 220, 20, 200, 40), $"AI : {_battle.RoundManager.AiHp}", bigStyle);

        string stateText = "";
        switch (_battle.Player.state)
        {
            case BattleState.ElementCharging:
                stateText = $"원소 : {_battle.Player.chargingElement}";
                break;
            case BattleState.FormCharging:
                stateText = $"{_battle.Player.confirmedElement} + 형태 : {_battle.Player.chargingForm}";
                break;
            case BattleState.Casting:
                stateText = "발동 중...";
                break;
        }
        GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 60, 200, 40), stateText, bigStyle);

        if (_battle.RoundManager.GameOver)
        {
            GUIStyle hugeStyle = new GUIStyle(GUI.skin.label);
            hugeStyle.fontSize = 60;
            hugeStyle.alignment = TextAnchor.MiddleCenter;
            string result = _battle.RoundManager.PlayerHp <= 0 ? "패배..." : "승리!";
            GUI.Label(new Rect(0, Screen.height / 2 - 40, Screen.width, 80), result, hugeStyle);
        }
    }
    
    private void DrawFullDebug()
    {
        float boxX = 10, boxY = 10;
        float boxW = 300, boxH = 400;
        GUI.Box(new Rect(boxX, boxY, boxW, boxH), "Hand Debug");
    
        float x = boxX + 10;
        float y = boxY + 25;
        float lineHeight = 22;
        float labelWidth = boxW - 20;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Left : {_recognizer.LeftWrist.x:F2}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"right : {_recognizer.RightWrist.x:F2}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Distance : {(_recognizer.WristDistance >= 0 ? _recognizer.WristDistance.ToString("F2") : "N/A")}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Stable : L={_recognizer.LeftStable} R={_recognizer.RightStable}");
        y += lineHeight;
    
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Pose : {_recognizer.CurrentPose}");
        y += lineHeight;
    
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"State : {_battle.Player.state}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Hold : {_battle.Player.holdTime:F2} / 1.00s");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Element : {_battle.Player.confirmedElement}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Form : {_battle.Player.confirmedForm}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"TotalTime : {_battle.Player.totalTime:F2}/10.00s");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"--- AI ---");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"AI State : {_battle.AI.ctx.state}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"AI Element : {_battle.AI.ctx.confirmedElement}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"AI Form : {_battle.AI.ctx.confirmedForm}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"AI Time : {_battle.AI.ctx.totalTime:F2}");
        y += lineHeight + 5;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"--- HP ---");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Player HP: {_battle.RoundManager.PlayerHp}");
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"AI HP: {_battle.RoundManager.AiHp}");
        
        if (_battle.RoundManager.GameOver)
        {
            y += lineHeight;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"=== GAME OVER ===");
        }
    }

}