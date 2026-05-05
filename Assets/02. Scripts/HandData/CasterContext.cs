using UnityEngine;
using static Constant;

public class CasterContext
{
    public HandPose chargingElement = HandPose.Unknown;
    public HandPose chargingForm = HandPose.Unknown;
    public HandPose confirmedElement = HandPose.Unknown;
    public HandPose confirmedForm = HandPose.Unknown;
    public float holdTime = 0f;
    public float totalTime = 0f;
    public BattleState state = BattleState.Idle;
}
