using System;
using UnityEngine;
using static Constant;

[CreateAssetMenu(fileName = "SoundData", menuName = "Game/Sound Data")]
public class SoundData : ScriptableObject
{
    [Serializable]
    public class BgmEntry
    {
        public BgmId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
    }

    [Serializable]
    public class SfxEntry
    {
        public SfxId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public BgmEntry[] bgmList;
    public SfxEntry[] sfxList;

    public BgmEntry GetBgm(BgmId id)
    {
        foreach(var entry in bgmList)
            if (entry.id == id) return entry;
        return null;
    }

    public SfxEntry GetSfx(SfxId id)
    {
        foreach(var entry in sfxList)
            if (entry.id == id) return entry;
        return null;
    }
}
