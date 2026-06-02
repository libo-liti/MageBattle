using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 메인 메뉴 화면.
    /// [게임시작] [설정] [종료] 세 개의 버튼을 처리하고
    /// 메인 패널과 설정 패널을 전환한다.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("패널 (Canvas 아래에 만들어 둔 GameObject 를 연결)")]
        [SerializeField] private GameObject mainPanel;     // 시작/설정/종료 버튼들이 들어있는 패널
        [SerializeField] private GameObject settingsPanel; // 사운드 슬라이더가 들어있는 패널

        [Header("메인 패널 버튼")]
        [SerializeField] private Button startButton;       // 게임 시작
        [SerializeField] private Button settingsButton;    // 설정 열기
        [SerializeField] private Button quitButton;        // 게임 종료

        [Header("설정 패널 버튼")]
        [SerializeField] private Button settingsBackButton; // 설정에서 뒤로가기

        [Header("게임 시작 시 불러올 씬 이름")]
        [SerializeField] private string gameSceneName = "SampleScene";

        [Header("로딩 화면 (같은 캔버스에 있는 LoadingScreen 컴포넌트)")]
        [SerializeField] private LoadingScreen loadingScreen;

        void Start()
        {
            // 각 버튼에 클릭 시 실행할 함수를 연결한다. (인스펙터에서 안 해도 됨)
            startButton.onClick.AddListener(OnStartClicked);
            settingsButton.onClick.AddListener(() => Show(settingsPanel));
            settingsBackButton.onClick.AddListener(() => Show(mainPanel));
            quitButton.onClick.AddListener(OnQuitClicked);

            // 처음에는 메인 패널만 보이게
            Show(mainPanel);
        }

        /// <summary>
        /// 둘 중 하나의 패널만 켜고 나머지는 끈다.
        /// </summary>
        void Show(GameObject panel)
        {
            mainPanel.SetActive(panel == mainPanel);
            settingsPanel.SetActive(panel == settingsPanel);
        }

        /// <summary>
        /// 시작 버튼: 로딩 화면을 띄우고 게임 씬을 비동기로 불러온다.
        /// </summary>
        void OnStartClicked()
        {
            loadingScreen.Load(gameSceneName);
        }

        /// <summary>
        /// 종료 버튼: 에디터에서는 플레이 모드 종료, 빌드에서는 앱 종료.
        /// </summary>
        void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
