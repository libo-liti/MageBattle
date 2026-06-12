using UnityEngine;

public class RoundResult
{
    public enum ResultType
    {
        Normal,
        Counter,
        Blocked,
        Failed
    }

    public ResultType type;
    public int dmgDealtToAi;
    public int dmgReceived;
}
