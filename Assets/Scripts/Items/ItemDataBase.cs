using UnityEngine;

using System.Collections.Generic;

[System.Serializable]
public class ItemDatabase
{
    public List<ItemData> items;
}

public enum ItemTier { T1, T2, T3 }
public enum ItemEffectType { Heal, AttackBoost, DefenseBoost }
