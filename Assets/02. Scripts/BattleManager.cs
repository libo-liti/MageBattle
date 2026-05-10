using static Constant;

public class BattleManager
{
    private const float REQUIRED_HOLD = 1.0f;
    
    private CasterContext _player = new CasterContext();
    private AICaster _ai = new AICaster();
    private RoundManager _roundManager;

    public CasterContext Player => _player;
    public AICaster AI => _ai;
    public RoundManager RoundManager => _roundManager;

    public BattleManager(RivalData rival = null)
    {
        _player = new CasterContext();
        _ai = new AICaster();
        _roundManager = new RoundManager(_player, _ai);
        
        if(rival != null)
            _ai.SetRivalData(rival);
        
        _roundManager.StartRound();
    }

    public void Update(HandPose pose, float deltaTime)
    {
        UpdateCaster(_player, pose, deltaTime);
        
        _ai.Update(deltaTime);
        _roundManager.Update(deltaTime);
    }
    
    private void UpdateCaster(CasterContext ctx, HandPose pose, float deltaTime)
    {
        switch (ctx.state)
        {
            case BattleState.Idle:
                if (IsElement(pose))
                {
                    ctx.state = BattleState.ElementCharging;
                    ctx.chargingElement = pose;
                    ctx.holdTime = 0f;
                    ctx.totalTime = 0;
                }
                break;
            case BattleState.ElementCharging:
                ctx.totalTime += deltaTime;
                
                if (IsElement(pose))
                {
                    if (pose == ctx.chargingElement)
                    {
                        ctx.holdTime += deltaTime;
                        if (ctx.holdTime >= REQUIRED_HOLD)
                        {
                            ctx.confirmedElement = ctx.chargingElement;
                            ctx.state = BattleState.FormCharging;
                            ctx.chargingForm = HandPose.Unknown;
                            ctx.holdTime = 0f;
                        }
                    }
                    else
                    {
                        ctx.chargingElement = pose;
                        ctx.holdTime = 0;
                    }
                }
                break;
            case BattleState.FormCharging:
                ctx.totalTime += deltaTime;

                if (IsElement(pose) && pose != ctx.confirmedElement)
                {
                    ctx.state = BattleState.ElementCharging;
                    ctx.chargingElement = pose;
                    ctx.confirmedElement = HandPose.Unknown;
                    ctx.chargingForm = HandPose.Unknown;
                    ctx.holdTime = 0f;
                    break;
                }
                
                if (IsForm(pose))
                {
                    if (ctx.chargingForm == HandPose.Unknown)
                    {
                        ctx.chargingForm = pose;
                        ctx.holdTime = 0;
                    }
                    else if (pose == ctx.chargingForm)
                    {
                        ctx.holdTime += deltaTime;
                        if (ctx.holdTime >= REQUIRED_HOLD)
                        {
                            ctx.confirmedForm = pose;
                            ctx.state = BattleState.Casting;
                            ctx.holdTime = 0f;
                        }
                    }
                    else
                    {
                        ctx.chargingForm = pose;
                        ctx.holdTime = 0f;
                    }
                }
                else
                {
                    ctx.chargingForm = HandPose.Unknown;
                    ctx.holdTime = 0f;
                }
                break;
            case BattleState.Casting:
                break;
        }
    }
    
    private bool IsElement(HandPose pose)
    {
        return pose == HandPose.Fire || pose == HandPose.Water || pose == HandPose.Wind || pose == HandPose.Land;
    }

    private bool IsForm(HandPose pose)
    {
        return pose == HandPose.Attack || pose == HandPose.Defense || pose == HandPose.Special;
    }
}
