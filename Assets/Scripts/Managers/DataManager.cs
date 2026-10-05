using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public AgentDatabase AgentDB { get; private set; }


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    private void LoadData()
    {
        AgentDB = JsonLoader.LoadJson<AgentDatabase>("Data/agents");
    }
    
    public List<AgentData> GetRandomAgents(int count)
    {
        List<AgentData> allAgents = new List<AgentData>(AgentDB.agents);

        if (allAgents.Count < count)
        {
            Debug.LogError("Not enough agents to choose from.");
            return allAgents;
        }

        // Fisher–Yates shuffle
        for (int i = 0; i < allAgents.Count; i++)
        {
            int randomIndex = Random.Range(i, allAgents.Count);
            (allAgents[i], allAgents[randomIndex]) = (allAgents[randomIndex], allAgents[i]);
        }

        return allAgents.GetRange(0, count);
    }
}
