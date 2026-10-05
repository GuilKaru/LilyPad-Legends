using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private SpawnManager spawnManager;

    private List<AgentRunTime> _activeAgents;

    public AgentRunTime LeftAgent => _activeAgents != null && _activeAgents.Count > 0 ? _activeAgents[0] : null;
    public AgentRunTime RightAgent => _activeAgents != null && _activeAgents.Count > 1 ? _activeAgents[1] : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Spawn the two agents
        _activeAgents = spawnManager.SpawnAgents(2);
        
        // Setup initial UI
        if (UIManager.Instance != null && LeftAgent != null && RightAgent != null)
        {
            //UIManager.Instance.InitializeDialogueFrogs(LeftAgent, RightAgent);
            UIManager.Instance.InitializeHealth(LeftAgent, RightAgent);
            UIManager.Instance.ShowPlayerNames();
            
            // Show buttons initially, but lock them until ActionManager says it's time
            UIManager.Instance.ShowAttackButtons(LeftAgent);
            UIManager.Instance.SetButtonsInteractable(false); 
            UIManager.Instance.ToggleActionPanel(false);
        }

        Debug.Log("[GameManager] Agents spawned. Awaiting state payloads from backend...");
    }

    public void ResetMatch()
    {
        Debug.Log("[GameManager] Resetting Match...");

        if (_activeAgents != null)
        {
            foreach (var agent in _activeAgents)
            {
                if (agent != null) Destroy(agent.gameObject);
            }
            _activeAgents.Clear();
        }
        
        ActionManager.Instance.ResetState();
        
        _activeAgents = spawnManager.SpawnAgents(2);

        // Re-initialize UI to idle state
        if (UIManager.Instance != null && LeftAgent != null && RightAgent != null)
        {
            UIManager.Instance.InitializeHealth(LeftAgent, RightAgent);
            UIManager.Instance.ShowPlayerNames();
            UIManager.Instance.ShowAttackButtons(LeftAgent);
            UIManager.Instance.SetButtonsInteractable(false);
            UIManager.Instance.ToggleActionPanel(false);
        }
        
        Debug.Log("[GameManager] Match reset. Awaiting new InitializeMatch call.");
    }
}