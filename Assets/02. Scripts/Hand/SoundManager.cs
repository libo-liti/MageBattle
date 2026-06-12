using UnityEngine;
using static Constant;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private SoundData soundData;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private BgmId _currentBgm = BgmId.None;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        bgmSource.loop = true;
    }

    public void PlayBgm(BgmId id)
    {
        if (_currentBgm == id) return;
        _currentBgm = id;

        if (id == BgmId.None)
        {
            bgmSource.Stop();
            return;
        }

        if (soundData == null) return;

        var entry = soundData.GetBgm(id);
        if (entry == null || entry.clip == null)
        {
            bgmSource.Stop();
            return;
        }

        bgmSource.clip = entry.clip;
        bgmSource.volume = entry.volume * bgmVolume * masterVolume;
        bgmSource.Play();
    }

    public void PlaySfx(SfxId id)
    {
        if (id == SfxId.None || soundData == null) return;

        var entry = soundData.GetSfx(id);
        if (entry == null || entry.clip == null)
            return;
        
        sfxSource.PlayOneShot(entry.clip, entry.volume * sfxVolume * masterVolume);
    }

    public void StopBgm()
    {
        _currentBgm = BgmId.None;
        bgmSource.Stop();
    }
}
