using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
    private static SaveData _cachedData;

    public static SaveData Data
    {
        get
        {
            if (_cachedData == null)
                Load();
            return _cachedData;
        }
    }

    public static void Load()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                _cachedData = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[Save] Loaded from {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Load failed: {e.Message}");
                _cachedData = new SaveData();
            }
        }
        else
        {
            _cachedData = new SaveData();
            Debug.Log("[Save] No save file, created new");
        }
    }

    public static void Save()
    {
        if (_cachedData == null) _cachedData = new SaveData();
        _cachedData.lastPlayedDate = DateTime.UtcNow.ToString("o");

        try
        {
            string json = JsonUtility.ToJson(_cachedData, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[Save] Saved to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Save failed: {e.Message}");
        }
    }

    public static bool IsRivalDefeated(string rivalId)
    {
        return Data.defeatedRivalIds.Contains(rivalId);
    }

    public static void MarkRivalDefeated(string rivalId)
    {
        if (!Data.defeatedRivalIds.Contains(rivalId))
        {
            Data.defeatedRivalIds.Add(rivalId);
            Save();
        }
    }

    public static void RecordGameResult(bool won)
    {
        Data.totalGamesPlayed++;
        if (won) Data.totalWins++;
        else Data.totalLosses++;
        Save();
    }

    public static void UnlockAll()
    {
        if(!Data.defeatedRivalIds.Contains("apprentice")) Data.defeatedRivalIds.Add("apprentice");
        if(!Data.defeatedRivalIds.Contains("master")) Data.defeatedRivalIds.Add("master");
        Save();
        Debug.Log("[Save] 모든 라이벌 해제");
    }

    public static void ResetAll()
    {
        _cachedData = new SaveData();
        if(File.Exists(SavePath)) File.Delete(SavePath);
        Debug.Log("[Save] 진행 초기화");
    }
}
