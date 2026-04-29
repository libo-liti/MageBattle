using UnityEngine;
using UnityEngine.Audio;

namespace Game.Audio
{
    /// <summary>
    /// 게임 전체의 사운드 볼륨을 관리하는 매니저.
    /// - 어디서든 AudioManager.Instance 로 접근할 수 있도록 싱글톤으로 만든다.
    /// - 볼륨 값은 PlayerPrefs(저장소)에 저장되어서 게임을 껐다 켜도 유지된다.
    /// - 실제 소리 크기 조절은 AudioMixer 가 담당한다.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // 다른 스크립트에서 AudioManager.Instance.SetBGM(0.5f) 처럼 쓰기 위한 싱글톤 참조
        public static AudioManager Instance { get; private set; }

        [Tooltip("Master/BGM/SFX 그룹이 있는 오디오 믹서를 연결하세요")]
        [SerializeField] private AudioMixer mixer;

        // PlayerPrefs 에 저장할 때 쓰는 키 이름. AudioMixer 의 노출 파라미터 이름과도 동일하게 맞춘다.
        const string MASTER = "Master";
        const string BGM    = "BGM";
        const string SFX    = "SFX";

        void Awake()
        {
            // 싱글톤 처리: 이미 있으면 자기 자신을 없애서 중복 방지
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 씬이 바뀌어도 살아남게 한다. (메인메뉴 → 게임씬으로 이동해도 볼륨 유지)
            DontDestroyOnLoad(gameObject);

            // 게임 시작할 때 저장된 볼륨을 불러와서 믹서에 적용
            // 처음 실행이면 기본값 1(=100%) 을 사용
            SetMaster(PlayerPrefs.GetFloat(MASTER, 1f));
            SetBGM   (PlayerPrefs.GetFloat(BGM,    1f));
            SetSFX   (PlayerPrefs.GetFloat(SFX,    1f));
        }

        // 현재 저장된 볼륨 값을 읽어가는 프로퍼티 (UI 슬라이더 초기화용)
        public float Master     => PlayerPrefs.GetFloat(MASTER, 1f);
        public float BGMVolume  => PlayerPrefs.GetFloat(BGM,    1f);
        public float SFXVolume  => PlayerPrefs.GetFloat(SFX,    1f);

        // 외부에서 슬라이더로 볼륨을 바꿀 때 호출하는 함수들
        public void SetMaster(float v) => Apply(MASTER, v);
        public void SetBGM   (float v) => Apply(BGM,    v);
        public void SetSFX   (float v) => Apply(SFX,    v);

        /// <summary>
        /// 슬라이더 값(0~1)을 받아서 1) PlayerPrefs 에 저장하고 2) AudioMixer 에 적용한다.
        /// AudioMixer 의 단위는 데시벨(dB)이라 Log10 변환이 필요하다.
        /// (선형 0.5 → 약 -6dB, 0.1 → -20dB)
        /// </summary>
        void Apply(string key, float linear)
        {
            // log10(0) = -무한대 라서 0이면 에러난다. 아주 작은 값으로 보정.
            linear = Mathf.Clamp(linear, 0.0001f, 1f);

            // 1) 저장 (메모리에 기록 + 즉시 디스크 flush)
            PlayerPrefs.SetFloat(key, linear);
            PlayerPrefs.Save();

            // 2) 믹서에 반영 (믹서 연결 안 했어도 에러 안 나게 null 체크)
            if (mixer != null)
            {
                float dB = Mathf.Log10(linear) * 20f;
                mixer.SetFloat(key, dB);
            }
        }
    }
}
