using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 씬을 비동기로 로딩하면서 진행률 바를 보여주는 로딩 화면.
    /// 너무 빨리 끝나면 어색하니까 최소 노출 시간을 보장한다.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [Header("로딩 UI")]
        [SerializeField] private GameObject root;       // 로딩 패널 (보였다 사라졌다 함)
        [SerializeField] private Slider progressBar;    // 0~1 진행률 바

        [Header("최소 로딩 시간(초). 너무 빨리 끝나는 것 방지")]
        [SerializeField] private float minDuration = 1f;

        void Awake()
        {
            // 처음에는 로딩 화면 숨김
            root.SetActive(false);
        }

        /// <summary>
        /// 외부에서 호출. 지정한 이름의 씬을 비동기 로딩한다.
        /// </summary>
        public void Load(string sceneName)
        {
            StartCoroutine(LoadRoutine(sceneName));
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            // 로딩 화면 켜고 진행률 0으로 초기화
            root.SetActive(true);
            progressBar.value = 0f;

            // 비동기 로딩 시작. allowSceneActivation=false 로 두면 99%에서 멈춘다.
            // → 우리가 원하는 타이밍에 화면 전환할 수 있게 함.
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            float elapsed = 0f;
            while (!op.isDone)
            {
                elapsed += Time.unscaledDeltaTime;

                // op.progress 는 0 ~ 0.9 범위 (0.9 가 로딩 완료)
                // 0~1 로 보이게 0.9 로 나눠줌
                float loadProgress = Mathf.Clamp01(op.progress / 0.9f);

                // 시간 기준 진행률 (최소 노출시간 채우기 위함)
                float timeProgress = Mathf.Clamp01(elapsed / minDuration);

                // 둘 중 느린 쪽을 보여줘서 너무 빨리 100% 가 되지 않게
                progressBar.value = Mathf.Min(loadProgress, timeProgress);

                // 로딩 완료 + 최소 시간도 지났으면 실제 씬 전환 허용
                if (op.progress >= 0.9f && elapsed >= minDuration)
                {
                    op.allowSceneActivation = true;
                }

                yield return null;
            }
        }
    }
}
