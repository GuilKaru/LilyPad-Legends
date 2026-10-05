using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Game Over UI")]
    [SerializeField] private TextMeshProUGUI winnerNameText;
    
    [SerializeField] private BackgroundManager backgroundManager;

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
        ShowMainMenu();
    }

    public void HideAllPanels()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (gameplayRoot) gameplayRoot.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void RequestMatch()
    {
        JSBridge.SendMatchRequest();
    }

    public void StartGame()
    {
        HideAllPanels();

        if (gameplayRoot)
            gameplayRoot.SetActive(true);
        
        if (backgroundManager)
        {
            backgroundManager.StartBackgroundSequence();
        }
    }

    public void ShowGameOverMenu(string winnerName)
    {
        //HideAllPanels();

        if (gameOverPanel)
            gameOverPanel.SetActive(true);

        if (winnerNameText)
            winnerNameText.text = $"{winnerName}";
    }

    public void PlayAgain()
    {
        HideAllPanels();
        
        if (gameplayRoot) gameplayRoot.SetActive(true);
        
        if (backgroundManager)
        {
            backgroundManager.ResetBackground();
            backgroundManager.StartBackgroundSequence();
        }
        
        GameManager.Instance.ResetMatch();
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game pressed.");
        Application.Quit();
    }
}
