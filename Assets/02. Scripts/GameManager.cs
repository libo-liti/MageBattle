using Pose.DetailedVisualizer;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Constant;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private RivalData _currentRival;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private GameObject stage;
    [SerializeField] private CasterMagicVfx playerVfx;
    [SerializeField] private CasterMagicVfx aiVfx;
    
    [Header("Hand Tracking")]
    [SerializeField] private HandVisualizer hand;
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject dojoSelectPanel;
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Result UI")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI subText;
    [SerializeField] private TextMeshProUGUI statValueText;
    [SerializeField] private Button nextButton;

    [Header("Tutorial")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TutorialConfig tutorialConfig;
    [SerializeField] private TutorialUIController tutorialUIController;

    [Header("UI Controller")]
    [SerializeField] private GameUIController uiController;

#if UNITY_EDITOR
    private bool _showDebug = false;
#endif
    private bool _prevState;
    
    private HandRecognizer _recognizer;
    private BattleManager _battle;
    private TutorialManager _tutorial;
    private GameState _state = GameState.MainMenu;

    private BattleState _prevPlayerState = BattleState.Idle;
    private BattleState _prevAiState = BattleState.Idle;
    
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
        cameraController.OnCameraSequenceComplete += HandleCameraComplete;
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_state == GameState.Playing)
                PauseGame();
            else if (_state == GameState.Pause)
                ResumeGame();
            else if (_state == GameState.Tutorial)
                StopTutorial();
        }
        
        if (_state == GameState.Playing)
        {
            _recognizer.Update();
            _battle.Update(_recognizer.CurrentPose, Time.deltaTime);
            
            uiController.Refresh(_battle, _recognizer);

            var currentPlayerState = _battle.Player.state;
            if (_prevPlayerState == BattleState.Idle && currentPlayerState == BattleState.ElementCharging)
            {
                // 영창 시작 — 손에 빛
                playerVfx.StartCharging(_battle.Player.chargingElement);
            }
            else if (_prevPlayerState == BattleState.ElementCharging && currentPlayerState == BattleState.FormCharging)
            {
                // Element 확정 — 색 변경
                playerVfx.StartCharging(_battle.Player.confirmedElement);
            }
            else if ((_prevPlayerState == BattleState.ElementCharging || _prevPlayerState == BattleState.FormCharging) 
                     && currentPlayerState == BattleState.Idle)
            {
                // 영창 취소 — 손 효과 끔
                playerVfx.StopCharging();
            }
            _prevPlayerState = currentPlayerState;

            var currentAiState = _battle.AI.ctx.state;          // ← AICaster API 확인 필요
            if (_prevAiState == BattleState.Idle && currentAiState == BattleState.ElementCharging)
            {
                aiVfx.StartCharging(_battle.AI.ctx.chargingElement);
            }
            else if (_prevAiState == BattleState.ElementCharging && currentAiState == BattleState.FormCharging)
            {
                aiVfx.StartCharging(_battle.AI.ctx.confirmedElement);
            }
            else if ((_prevAiState == BattleState.ElementCharging || _prevAiState == BattleState.FormCharging)
                     && currentAiState == BattleState.Idle)
            {
                aiVfx.StopCharging();
            }
            _prevAiState = currentAiState;
            
            if(_battle.RoundManager.GameOver)
                ShowGameOver();
        }

        if (_state == GameState.Tutorial)
        {
            _recognizer.Update();
            BattleManager.UpdateCaster(_tutorial.Player, _recognizer.CurrentPose, Time.deltaTime);
            _tutorial.Update(Time.deltaTime);

            var currentPlayerState = _tutorial.Player.state;
            if (_prevPlayerState == BattleState.Idle && currentPlayerState == BattleState.ElementCharging)
                playerVfx.StartCharging(_tutorial.Player.chargingElement);
            else if (_prevPlayerState == BattleState.ElementCharging && currentPlayerState == BattleState.FormCharging)
                playerVfx.StartCharging(_tutorial.Player.confirmedElement);
            else if ((_prevPlayerState == BattleState.ElementCharging || _prevPlayerState == BattleState.FormCharging)
                     && currentPlayerState == BattleState.Idle)
                playerVfx.StopCharging();
            _prevPlayerState = currentPlayerState;

            var currentAiState = _tutorial.AI.ctx.state;
            if (_prevAiState == BattleState.Idle && currentAiState == BattleState.ElementCharging)
                aiVfx.StartCharging(_tutorial.AI.ctx.chargingElement);
            else if (_prevAiState == BattleState.ElementCharging && currentAiState == BattleState.FormCharging)
                aiVfx.StartCharging(_tutorial.AI.ctx.confirmedElement);
            else if ((_prevAiState == BattleState.ElementCharging || _prevAiState == BattleState.FormCharging)
                     && currentAiState == BattleState.Idle)
                aiVfx.StopCharging();
            _prevAiState = currentAiState;

            bool active = _tutorial.CurrentMissionData != null;
            tutorialUIController.Refresh(
                active ? _tutorial.Player : null,
                active ? _tutorial.AI    : null);
        }
        
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
            _showDebug = !_showDebug;
        if (Input.GetKeyDown(KeyCode.F2))
            SaveSystem.UnlockAll();
        if (Input.GetKeyDown(KeyCode.F3))
            SaveSystem.ResetAll();
#endif
    }

    public void PauseGame()
    {
        _state = GameState.Pause;
        pauseMenuPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        _state = GameState.Playing;
        pauseMenuPanel.SetActive(false);
    }
    
    public void OnResumeClicked()
    {
        ResumeGame();
    }

    public void OnPauseMenuClicked()
    {
        pauseMenuPanel.SetActive(false);
        ShowMainMenu();
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
        stage.SetActive(false);
        pauseMenuPanel.SetActive(false);
        tutorialPanel.SetActive(false);
    }
    
    public void ShowDojoSelect()
    {
        _state = GameState.MainMenu;
        mainMenuPanel.SetActive(false);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        dojoSelectPanel.SetActive(true);
        pauseMenuPanel.SetActive(false);
        tutorialPanel.SetActive(false);
    }

    public void StartGame()
    {
        _state = GameState.Playing;
        mainMenuPanel.SetActive(false);
        gamePanel.SetActive(true);
        gameOverPanel.SetActive(false);
        dojoSelectPanel.SetActive(false);
        stage.SetActive(true);
        pauseMenuPanel.SetActive(false);

        // 이전 게임의 토스트 잔류 제거
        if (uiController != null) uiController.HideToast();

        UnsubscribeFromBattle();
        _battle = new BattleManager(_currentRival);
        SubscribeToBattle();

        // 페르소나 초기화 + 게임 시작 도발
        if (PersonaManager.Instance != null)
        {
            PersonaManager.Instance.SetCurrentRival(_currentRival);
            PersonaManager.Instance.TriggerPersona(PersonaManager.TRIG_GAME_START);
        }
    }

    private void OnRoundResolved(RoundResult result)
    {
        cameraController.ShowThirdPersonFor(CalcCamDuration(result));
        uiController.ShowToast(result);
        HandlePostRoundVfx(result);
    }

    private float CalcCamDuration(RoundResult result)
    {
        var player = _battle.Player;
        var ai     = _battle.AI.ctx;
        float ft   = playerVfx.ProjectileFlightTime;

        bool playerCounterSuccess = player.confirmedForm == HandPose.Special
            && result.type == RoundResult.ResultType.Counter
            && result.dmgDealtToAi > result.dmgReceived;

        bool aiCounterSuccess = ai.confirmedForm == HandPose.Special
            && result.type == RoundResult.ResultType.Counter
            && result.dmgReceived > result.dmgDealtToAi;

        // Counter 성공: AI투사체(ft) + 흡수대기(0.3s) + 반사투사체(ft) + 여유(0.6s)
        if (playerCounterSuccess || aiCounterSuccess)
            return ft * 2f + 0.3f + 0.6f;

        // 일반 공격 or 쉴드 히트: 투사체 비행(ft) + 여유(0.6s)
        bool anyProjectile =
            (player.confirmedForm == HandPose.Attack || player.confirmedForm == HandPose.Special)
         || (ai.confirmedForm    == HandPose.Attack || ai.confirmedForm    == HandPose.Special);
        if (anyProjectile)
            return ft + 0.6f;

        // Defense만 or 아무 공격 없음
        return 1.2f;
    }

    private void HandlePostRoundVfx(RoundResult result)
    {
        var player = _battle.Player;
        var ai     = _battle.AI.ctx;

        bool playerAttacks = player.confirmedElement != HandPose.Unknown
                             && player.confirmedForm == HandPose.Attack;
        bool playerDefends = player.confirmedForm == HandPose.Defense;
        bool playerSpecial = player.confirmedForm == HandPose.Special;

        bool aiAttacks = ai.confirmedElement != HandPose.Unknown
                         && ai.confirmedForm == HandPose.Attack;
        bool aiDefends = ai.confirmedForm == HandPose.Defense;
        bool aiSpecial = ai.confirmedForm == HandPose.Special;

        bool playerCounterSuccess = playerSpecial
            && result.type == RoundResult.ResultType.Counter
            && result.dmgDealtToAi > result.dmgReceived;

        bool aiCounterSuccess = aiSpecial
            && result.type == RoundResult.ResultType.Counter
            && result.dmgReceived > result.dmgDealtToAi;

        playerVfx.StopCharging();
        aiVfx.StopCharging();

        // ── 쉴드는 투사체보다 먼저 표시 ──────────────────────
        if (playerDefends) playerVfx.ShowShield(player.confirmedElement);
        if (aiDefends)     aiVfx.ShowShield(ai.confirmedElement);

        bool collisionHandled = false;

        // ── 플레이어 VFX ──────────────────────────────────────
        if (playerAttacks)
        {
            if (aiDefends)
            {
                playerVfx.FireAtShield(player.confirmedElement, aiVfx);
            }
            else if (aiAttacks)
            {
                // 양쪽 모두 공격 — 원소 상성에 따라 충돌 처리
                float mult = DamageCalculator.GetElementMultiplier(
                    player.confirmedElement, ai.confirmedElement);

                if (mult == 0f)
                {
                    // 같은 원소 → 양쪽 중간 상쇄
                    playerVfx.FireCancelled(player.confirmedElement);
                    aiVfx.FireCancelled(ai.confirmedElement);
                }
                else if (mult > 1f)
                {
                    // 플레이어 강함 → AI 투사체 중간 소멸, 플레이어 계속
                    aiVfx.FireCancelled(ai.confirmedElement);
                    playerVfx.Fire(player.confirmedElement, result.dmgDealtToAi > 0);
                }
                else if (mult < 1f)
                {
                    // AI 강함 → 플레이어 투사체 중간 소멸, AI 계속
                    playerVfx.FireCancelled(player.confirmedElement);
                    aiVfx.Fire(ai.confirmedElement, result.dmgReceived > 0);
                }
                else
                {
                    // 무관 (1.0×) → 양쪽 통과
                    playerVfx.Fire(player.confirmedElement, result.dmgDealtToAi > 0);
                    aiVfx.Fire(ai.confirmedElement, result.dmgReceived > 0);
                }

                collisionHandled = true;
            }
            else
            {
                playerVfx.Fire(player.confirmedElement, result.dmgDealtToAi > 0);
            }
        }
        else if (playerSpecial)
        {
            if (playerCounterSuccess)
                playerVfx.ShowCounterAndReflect(player.confirmedElement);
            else
                playerVfx.ShowCounterFail(player.confirmedElement);
        }

        // ── AI VFX ────────────────────────────────────────────
        if (aiAttacks && !collisionHandled)
        {
            if (playerDefends)
                aiVfx.FireAtShield(ai.confirmedElement, playerVfx);
            else if (playerCounterSuccess)
                aiVfx.Fire(ai.confirmedElement, false);  // Counter가 흡수
            else
                aiVfx.Fire(ai.confirmedElement, result.dmgReceived > 0);
        }
        else if (aiSpecial)
        {
            if (aiCounterSuccess)
                aiVfx.ShowCounterAndReflect(ai.confirmedElement);
            else
                aiVfx.ShowCounterFail(ai.confirmedElement);
        }
    }

    public void ShowGameOver()
    {
        _state = GameState.GameOver;
        gameOverPanel.SetActive(true);

        bool playerWon = _battle.RoundManager.AiHp <= 0;
        resultText.text = playerWon ? "승리!" : "패배...";

        // 페르소나 트리거: 라이벌의 마지막 대사
        PersonaManager.Instance?.TriggerPersona(
            playerWon ? PersonaManager.TRIG_AI_DEFEAT : PersonaManager.TRIG_AI_VICTORY);

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
        _battle.RoundManager.OnRoundResolved += OnRoundResolved;
    }

    private void UnsubscribeFromBattle()
    {
        if (_battle != null)
        {
            _battle.RoundManager.OnRoundResolved -= uiController.ShowToast;
            _battle.RoundManager.OnRoundResolved -= OnRoundResolved;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromBattle();

        if (cameraController != null)
            cameraController.OnCameraSequenceComplete -= HandleCameraComplete;
    }

    private void HandleCameraComplete()
    {
        if(_battle != null)
            _battle.RoundManager.RequestNextRound();
    }
    public void StartTutorial()
    {
        _state = GameState.Tutorial;
        mainMenuPanel.SetActive(false);
        tutorialPanel.SetActive(true);
        gamePanel.SetActive(false);
        dojoSelectPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
        stage.SetActive(true);

        var player = new CasterContext();
        var ai = new AICaster();
        _tutorial = new TutorialManager(player, ai, tutorialConfig);
        tutorialUIController.Init(_tutorial, _recognizer, tutorialConfig.missions.Length);
        _tutorial.Start();
    }

    public void StopTutorial()
    {
        playerVfx.StopCharging();
        aiVfx.StopCharging();
        tutorialUIController.Cleanup(_tutorial);
        _tutorial = null;
        ShowMainMenu();
    }

    public void OnTutorialClicked()     // 메인 메뉴: 수련
    {
        SoundManager.Instance?.PlaySfx(SfxId.UiClick);
        StartTutorial();
    }

    public void OnDojoBreakClicked()    // 메인 메뉴: 도장 깨기
    {   
        SoundManager.Instance.PlaySfx(SfxId.UiClick);
        ShowDojoSelect();
    }
    
    public void OnDojoBackClicked()
    {
        SoundManager.Instance.PlaySfx(SfxId.UiClick);
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
    
#if UNITY_EDITOR
    private void OnGUI()
    {
        if (_showDebug)
            DrawFullDebug();
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
#endif

}