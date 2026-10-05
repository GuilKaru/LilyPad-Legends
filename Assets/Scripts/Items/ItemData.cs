using System;
using UnityEngine;

[System.Serializable]
public class ItemData
{
    public string id;
    public string name;

    // Raw string from JSON
    [SerializeField] private string tier;
    [SerializeField] private string effectTypeRaw;

    public float weight;

    public float healPercent;
    public float attackBoost;
    public float defenseBoost;

    // Convert string → enum safely (case-insensitive)
    public ItemTier TierEnum =>
        Enum.TryParse(tier, true, out ItemTier parsed)
            ? parsed
            : ItemTier.T1;

    public ItemEffectType EffectTypeEnum =>
        Enum.TryParse(effectTypeRaw, true, out ItemEffectType parsed)
            ? parsed
            : ItemEffectType.Heal;
}


