using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AgentDatabase
{
    public List<AgentData> agents;
}

[System.Serializable]
public class AgentData
{
    public string id;
    public string name;
    public AgentStats stats;
    public List<string> attacks;
    
    public string visualId;
    public string visualHeadId;
    public string visualMouthId;
    public string visualEyesId;
    public string visualHatId;
}

[System.Serializable]
public class AgentStats
{
    public int maxHealth;
    public int attack;
    public int defense;
    public int speed;
    public float criticalChance;
}