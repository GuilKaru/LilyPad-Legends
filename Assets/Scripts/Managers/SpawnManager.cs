using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    //Inspector
    #region Inspector

    [SerializeField] private GameObject agentPrefab;

    #endregion
    
    //Public API
    #region Public API
    
    /// Spawns a given number of random, non-repeating agents.
    public List<AgentRunTime> SpawnAgents(int count)
    {
        List<AgentRunTime> spawnedAgents = new();

        List<AgentData> selectedAgents = DataManager.Instance.GetRandomAgents(count);

        if (selectedAgents.Count < count)
        {
            Debug.LogError("Not enough agents returned by DataManager.");
            return spawnedAgents;
        }
        
        // Notice: We removed the speed sorting here. The backend determines turn order.

        Vector3 leftSpawn  = new(-5f, -0.75f, 0f);
        Vector3 rightSpawn = new( 5f, -0.75f, 0f);

        for (int i = 0; i < selectedAgents.Count; i++)
        {
            Vector3 spawnPos = (i == 0) ? leftSpawn : rightSpawn;
            AgentRunTime agent = SpawnSingleAgent(selectedAgents[i], spawnPos);
            spawnedAgents.Add(agent);
        }

        return spawnedAgents;
    }

    #endregion
    
    //Internal Logic
    #region Internal Logic
    
    /// Spawns a single agent at a given position, assigns visuals and data.
    private AgentRunTime SpawnSingleAgent(AgentData agentData, Vector3 position)
    {
        GameObject obj = Instantiate(agentPrefab, position, Quaternion.identity);

        AgentRunTime agent = obj.GetComponent<AgentRunTime>();
        if (agent == null)
        {
            Debug.LogError("AgentPrefab missing AgentRunTime!");
            return null;
        }

        // Notice: We removed AttackDB from the Initialize call
        agent.Initialize(agentData);

        // Flip sprite if this is the left-side agent
        if (position.x < 0)
        {
            agent.spriteHolder.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            agent.spriteHolder.localScale = new Vector3(1, 1, 1);
        }

        return agent;
    }

    #endregion
}
