using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public List<string> defeatedRivalIds = new List<string>();
    public int totalGamesPlayed = 0;
    public int totalWins = 0;
    public int totalLosses = 0;
    public string lastPlayedDate;  // ISO 8601 형식
    
    // 미래 확장 — 라이벌별 전적
    // public Dictionary<string, RivalRecord> rivalRecords; 
}
