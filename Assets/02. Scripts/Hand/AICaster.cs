using static Constant;
using Random = UnityEngine.Random;

public class AICaster
{
    public CasterContext ctx { get; }

    private float _plannedDuration;
    private HandPose _plannedElement;
    private HandPose _plannedForm;
    private bool _roundActive;

    private float _aiTimeMin = 4f;
    private float _aiTimeMax = 7f;

    private float _attackWeight = 1f;
    private float _defenseWeight = 1f;
    private float _specialWeight = 1f;

    public AICaster()
    {
        ctx = new CasterContext();
    }

    public void StartRound()
    {
        _plannedElement = RandomElement();
        _plannedForm = RandomForm();

        _plannedDuration = Random.Range(_aiTimeMin, _aiTimeMax);

        ctx.state = BattleState.ElementCharging;
        ctx.chargingElement = _plannedElement;
        ctx.totalTime = 0;

        _roundActive = true;
    }

    public void StartRound(HandPose element, HandPose form, float duration)
    {
        _plannedElement = element;
        _plannedForm = form;
        _plannedDuration = duration;

        ctx.state = BattleState.ElementCharging;
        ctx.chargingElement = _plannedElement;
        ctx.totalTime = 0;
        _roundActive = true;
    }

    public void StartIdleRound()
    {
        _roundActive = false;
        ctx.state = BattleState.Idle;
    }

    public void Update(float deltaTime)
    {
        if (!_roundActive) return;

        ctx.totalTime += deltaTime;

        if (ctx.state == BattleState.ElementCharging && ctx.totalTime >= _plannedDuration / 2)
        {
            ctx.confirmedElement = _plannedElement;
            ctx.state = BattleState.FormCharging;
            ctx.chargingForm = _plannedForm;
        }

        if (ctx.state == BattleState.FormCharging && ctx.totalTime >= _plannedDuration)
        {
            ctx.confirmedForm = _plannedForm;
            ctx.state = BattleState.Casting;
            _roundActive = false;
        }
    }

    public void SetRivalData(RivalData rival)
    {
        _aiTimeMin = rival.aiTimeMin;
        _aiTimeMax = rival.aiTimeMax;
        _attackWeight = rival.attackWeight;
        _defenseWeight = rival.defenseWeight;
        _specialWeight = rival.specialWeight;
    }

    private HandPose RandomElement()
    {
        HandPose[] elements = { HandPose.Fire, HandPose.Water, HandPose.Wind, HandPose.Land };
        return elements[Random.Range(0, 4)];
    }

    private HandPose RandomForm()
    {
        float total = _attackWeight + _defenseWeight + _specialWeight;
        if (total <= 0f)
        {
            HandPose[] forms = { HandPose.Attack, HandPose.Defense, HandPose.Special };
            return forms[Random.Range(0, 3)];
        }

        float roll = Random.value * total;
        if (roll < _attackWeight) return HandPose.Attack;
        if (roll < _attackWeight + _defenseWeight) return HandPose.Defense;
        return HandPose.Special;
    }

    public void Reset()
    {
        ctx.state = BattleState.Idle;
        ctx.chargingElement = HandPose.Unknown;
        ctx.chargingForm = HandPose.Unknown;
        ctx.confirmedElement = HandPose.Unknown;
        ctx.confirmedForm = HandPose.Unknown;
        ctx.totalTime = 0;
        ctx.holdTime = 0;
        _roundActive = false;
    }
}
