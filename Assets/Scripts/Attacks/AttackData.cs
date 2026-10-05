using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackDatabase
{
    public List<AttackData> attacks;
}

[System.Serializable]
public class AttackData
{
    public string id;
    public string name;
    
    // Read from JSON as string
    [SerializeField] private string type;

    // Exposed enum (used everywhere else)
    public AttackType TypeEnum
    {
        get
        {
            if (Enum.TryParse(type, true, out AttackType parsed))
                return parsed;

            Debug.LogWarning($"Unknown AttackType '{type}' on attack '{name}', defaulting to Damage.");
            return AttackType.Damage;
        }
    }

    // Damage values (Damage / Charge / Risky)
    public int minPower;
    public int maxPower;
    public float accuracy;

    // Limited uses (Charge / Protect)
    public int maxUses;

    // Charge only
    public int chargeTurns;

    // Chance-based self effects (Charge / Risky)
    public float selfEffectChance;   // 0.0 – 1.0
    public SelfEffects selfEffects;

    // Protect only
    public List<float> accuracyStages;
}

[System.Serializable]
public class SelfEffects
{
    public float attackModifier = 1f;
    public float defenseModifier = 1f;
}

public enum AttackType
{
    Damage,
    Charge,
    Protect
}
