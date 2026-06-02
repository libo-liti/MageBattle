using Game.Audio;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Game.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        [Header("볼륨 슬라이더 (0~1)")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("화면")]
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Dropdown resolutionDropdown;


        private List<Resolution> _resolutions;
        private bool _isFullscreen;

        void OnEnable()
        {
            // Screen.fullScreen은 비동기 반영이라 패널 열릴 때 현재 상태를 직접 읽어 캐싱
            _isFullscreen = Screen.fullScreen;

            // ── 사운드 ──────────────────────────────────────
            var audio = AudioManager.Instance;
            if (audio != null)
            {
                masterSlider.SetValueWithoutNotify(audio.Master);
                bgmSlider.SetValueWithoutNotify(audio.BGMVolume);
                sfxSlider.SetValueWithoutNotify(audio.SFXVolume);
                masterSlider.onValueChanged.AddListener(audio.SetMaster);
                bgmSlider.onValueChanged.AddListener(audio.SetBGM);
                sfxSlider.onValueChanged.AddListener(audio.SetSFX);
            }

            // ── 화면 ────────────────────────────────────────
            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(_isFullscreen);
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            if (resolutionDropdown != null)
            {
                InitResolutionDropdown();
                UpdateResolutionDropdownInteractable();
            }
        }

        void OnDisable()
        {
            masterSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.RemoveAllListeners();
            fullscreenToggle?.onValueChanged.RemoveAllListeners();
            resolutionDropdown?.onValueChanged.RemoveAllListeners();
        }

        private void InitResolutionDropdown()
        {
            // 사람들이 자주 쓰는 해상도만 표시
            int[][] common = new int[][] {
                new int[] { 1280,  720 },
                new int[] { 1600,  900 },
                new int[] { 1920, 1080 },
                new int[] { 2560, 1440 },
                new int[] { 3840, 2160 },
            };

            _resolutions = new List<Resolution>();
            var available = new HashSet<string>();
            foreach (var r in Screen.resolutions)
                available.Add(r.width + "x" + r.height);

            foreach (int[] wh in common)
            {
                if (available.Contains(wh[0] + "x" + wh[1]))
                {
                    var r = new Resolution();
                    r.width  = wh[0];
                    r.height = wh[1];
                    _resolutions.Add(r);
                }
            }
            // 현재 해상도가 목록에 없으면 추가
            bool hasCurrent = false;
            foreach (var r in _resolutions)
                if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
                    hasCurrent = true;
            if (!hasCurrent)
                _resolutions.Add(Screen.currentResolution);

            resolutionDropdown.ClearOptions();
            var options = new List<string>();
            int savedW = PlayerPrefs.GetInt("ResolutionW", Screen.currentResolution.width);
            int savedH = PlayerPrefs.GetInt("ResolutionH", Screen.currentResolution.height);
            int currentIndex = 0;

            for (int i = 0; i < _resolutions.Count; i++)
            {
                options.Add($"{_resolutions[i].width} × {_resolutions[i].height}");
                if (_resolutions[i].width == savedW && _resolutions[i].height == savedH)
                    currentIndex = i;
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.SetValueWithoutNotify(currentIndex);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            _isFullscreen = isFullscreen;
            if (isFullscreen)
            {
                // 전체화면: 네이티브 해상도로 강제 복원
                var native = Screen.currentResolution;
                Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
                // 드롭다운도 네이티브 해상도 항목으로 맞춤
                SyncDropdownTo(native.width, native.height);
            }
            else
            {
                // 창모드 복귀: 저장된 해상도 복원
                int w = PlayerPrefs.GetInt("ResolutionW", Screen.currentResolution.width);
                int h = PlayerPrefs.GetInt("ResolutionH", Screen.currentResolution.height);
                Screen.SetResolution(w, h, FullScreenMode.Windowed);
                SyncDropdownTo(w, h);
            }
            PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
            UpdateResolutionDropdownInteractable();
        }

        private void SyncDropdownTo(int w, int h)
        {
            if (_resolutions == null || resolutionDropdown == null) return;
            for (int i = 0; i < _resolutions.Count; i++)
            {
                if (_resolutions[i].width == w && _resolutions[i].height == h)
                {
                    resolutionDropdown.SetValueWithoutNotify(i);
                    return;
                }
            }
        }

        private void UpdateResolutionDropdownInteractable()
        {
            if (resolutionDropdown == null) return;
            resolutionDropdown.interactable = !_isFullscreen;
            var label = resolutionDropdown.transform.Find("Label")?.GetComponent<UnityEngine.UI.Text>();
            if (label != null) label.color = !_isFullscreen ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        private void OnResolutionChanged(int index)
        {
            if (_resolutions == null || index >= _resolutions.Count) return;
            var r = _resolutions[index];
            Screen.SetResolution(r.width, r.height, FullScreenMode.Windowed);
            PlayerPrefs.SetInt("ResolutionW", r.width);
            PlayerPrefs.SetInt("ResolutionH", r.height);
            PlayerPrefs.Save();
        }

    }
}
