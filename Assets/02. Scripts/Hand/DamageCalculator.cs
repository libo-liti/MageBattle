using static Constant;

public static class DamageCalculator
{
    public const int BASE_DAMAGE = 20;

    public static int Calculate(
        HandPose attackerEl, HandPose attackerForm,
        HandPose defenderEl, HandPose defenderForm,
        HandPose attackerPrevE1)
    {
        if (attackerForm == HandPose.Defense) return 0;
        if (attackerForm == HandPose.Special) return 0;

        float damage = BASE_DAMAGE;
        damage *= GetElementMultiplier(attackerEl, defenderEl);

        if (defenderForm == HandPose.Defense) damage *= 0.5f;
        if (attackerEl == attackerPrevE1) damage *= 0.7f;

        return (int)damage;
    }

    public static float GetElementMultiplier(HandPose attacker, HandPose defender)
    {
        if (attacker == HandPose.Unknown || defender == HandPose.Unknown) return 1.0f;
        if (attacker == defender) return 0f;
        if (GetStrongAgainst(attacker) == defender) return 1.5f;
        if (GetWeakAgainst(attacker) == defender) return 0.5f;
        return 1.0f;
    }

    public static HandPose GetStrongAgainst(HandPose element)
    {
        switch (element)
        {
            case HandPose.Fire: return HandPose.Wind;
            case HandPose.Water: return HandPose.Fire;
            case HandPose.Wind: return HandPose.Land;
            case HandPose.Land: return HandPose.Water;
            default: return HandPose.Unknown;
        }
    }

    public static HandPose GetWeakAgainst(HandPose element)
    {
        switch (element)
        {
            case HandPose.Fire: return HandPose.Water;
            case HandPose.Water: return HandPose.Land;
            case HandPose.Wind: return HandPose.Fire;
            case HandPose.Land: return HandPose.Wind;
            default: return HandPose.Unknown;
        }
    }
}
