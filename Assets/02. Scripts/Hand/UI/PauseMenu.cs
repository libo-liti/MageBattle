using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 인게임 일시정지 메뉴. ESC 키로 켜고 끈다.
    /// 일시정지 중에는 Time.timeScale 을 0 으로 만들어 게임 진행을 멈춘다.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("패널")]
        [SerializeField] private GameObject pausePanel;     // [계속하기/설정/메인메뉴] 버튼이 있는 패널
        [SerializeField] private GameObject settingsPanel;  // 사운드 설정 패널 (메인메뉴와 같은 모양)

        [Header("일시정지 패널 버튼")]
        [SerializeField] private Button resumeButton;       // 게임 계속하기
        [SerializeField] private Button settingsButton;     // 설정 열기
        [SerializeField] private Button mainMenuButton;     // 메인 메뉴로 돌아가기

        [Header("설정 패널 버튼")]
        [SerializeField] private Button settingsBackButton; // 설정에서 뒤로

        [Header("메인 메뉴 씬 이름")]
        [SerializeField] private string mainMenuScene = "MainMenu";

        // 현재 일시정지 상태인지 기억
        bool isPaused;

        void Start()
        {
            // 버튼 클릭 이벤트 연결
            resumeButton.onClick.AddListener(Resume);
            settingsButton.onClick.AddListener(() => ShowSettings(true));
            settingsBackButton.onClick.AddListener(() => ShowSettings(false));
            mainMenuButton.onClick.AddListener(GoToMainMenu);

            // 처음엔 둘 다 꺼두기
            pausePanel.SetActive(false);
            settingsPanel.SetActive(false);
        }

        void Update()
        {
            // ESC 키 처리 (새 Input System)
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (settingsPanel.activeSelf)
                {
                    // 설정 화면 → 일시정지 화면으로 돌아가기
                    ShowSettings(false);
                }
                else if (isPaused)
                {
                    // 일시정지 중 → 게임 재개
                    Resume();
                }
                else
                {
                    // 게임 중 → 일시정지
                    Pause();
                }
            }
        }

        /// <summary>일시정지 시작: 시간 정지 + 패널 켜기</summary>
        void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;       // 게임 시간 정지 (애니메이션/물리 등)
            pausePanel.SetActive(true);
        }

        /// <summary>일시정지 해제: 시간 복구 + 패널 끄기</summary>
        void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;
            pausePanel.SetActive(false);
            settingsPanel.SetActive(false);
        }

        /// <summary>설정 화면을 켜거나 끈다. (일시정지 패널과 토글)</summary>
        void ShowSettings(bool on)
        {
            settingsPanel.SetActive(on);
            pausePanel.SetActive(!on);
        }

        /// <summary>메인 메뉴 씬으로 이동. 시간 복구 잊지 말기.</summary>
        void GoToMainMenu()
        {
            Time.timeScale = 1f; // 안 풀면 메인 메뉴도 멈춰서 동작 안 함
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}
