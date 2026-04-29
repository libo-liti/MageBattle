using Game.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 사운드 설정 패널. Master / BGM / SFX 슬라이더 3개를 AudioManager 에 연결한다.
    /// 메인메뉴와 일시정지 메뉴 양쪽에서 똑같이 사용할 수 있다.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("볼륨 슬라이더 (0~1 범위로 설정)")]
        [SerializeField] private Slider masterSlider; // 전체 볼륨
        [SerializeField] private Slider bgmSlider;    // 배경음
        [SerializeField] private Slider sfxSlider;    // 효과음

        // 패널이 켜질 때마다 호출됨 → 현재 저장된 볼륨으로 슬라이더를 맞추고 이벤트 연결
        void OnEnable()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return; // 매니저가 없으면 아무것도 안 함

            // SetValueWithoutNotify: 슬라이더 값을 설정하면서도 onValueChanged 가 안 불리게 함.
            // (그냥 .value = ... 하면 변경 이벤트가 발생해서 무한 루프 위험)
            masterSlider.SetValueWithoutNotify(audio.Master);
            bgmSlider.SetValueWithoutNotify(audio.BGMVolume);
            sfxSlider.SetValueWithoutNotify(audio.SFXVolume);

            // 슬라이더를 움직이면 AudioManager 의 함수가 바로 호출되도록 연결
            masterSlider.onValueChanged.AddListener(audio.SetMaster);
            bgmSlider.onValueChanged.AddListener(audio.SetBGM);
            sfxSlider.onValueChanged.AddListener(audio.SetSFX);
        }

        // 패널이 꺼질 때 이벤트 연결을 끊어준다. (중복 연결 방지)
        void OnDisable()
        {
            masterSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.RemoveAllListeners();
        }
    }
}
