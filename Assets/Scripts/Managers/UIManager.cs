using System;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;

// Manages the battle UI, including attack buttons, health bars, and UI animations.
public class UIManager : MonoBehaviour
{
   // Singleton
    public static UIManager Instance { get; private set; }

    [Header("Attack Buttons")]
    public GameObject attackPanel;
    public Button fastAttackButtonPrefab;
    public Button geyserButtonPrefab;
    public Button defendButtonPrefab;
    public Button callBluffButtonPrefab;
    public Button noCallButtonPrefab;
    public Button claimSpecialButtonPrefab;
    public Button claimDefenseButtonPrefab;

    [Header("Panels")]
    public GameObject AttackPanel;
    public GameObject CallOrClaimPanel;
    public TextMeshProUGUI callOrClaimLabel;
    
    [Header("Health Bars")]
    public Slider leftHealthBar;
    public Slider rightHealthBar;
    [SerializeField] private float healthLerpSpeed = 8f;
    
    [Header("Health Text")]
    public TextMeshProUGUI leftHealthText;
    public TextMeshProUGUI rightHealthText;

    [Header("Turn Indicators")]
    public TextMeshProUGUI leftTurnText;
    public TextMeshProUGUI rightTurnText;
    public TextMeshProUGUI roundText;

    [Header("Dialogue UI")]
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI nameText;
    public CanvasGroup dialogueGroup;

    [Header("FP UI")] 
    // LEFT PLAYER
    public TextMeshProUGUI fpLeftText;
    public GameObject[] frogPointsLeft = new GameObject[8];

    // RIGHT PLAYER
    public TextMeshProUGUI fpRightText;
    public GameObject[] frogPointsRight = new GameObject[8];
    [Header("Bluff UI")]
    public TextMeshProUGUI bluffInstructionText;

    [Header("Bluff Result UI - Sprites")]
    public Sprite signalDefendSprite;
    public Sprite signalSpecialSprite;
    public Sprite unknownSprite;
    public Sprite callSprite;
    public Sprite checkSprite;
    public Sprite crossSprite;
    public Sprite truthBorderSprite;
    public Sprite lieBorderSprite;
    public Sprite neutralBorderSprite;
    public Sprite neutralActualBorderSprite;

    [Header("Bluff Result UI - Left")]
    public Image leftClaimImage;
    public Image leftClaimBorder;
    public Image leftActualActionImage;
    public Image leftActualActionBorder;
    public Image leftCallImage;
    public Image leftCallCheckImage;

    [Header("Bluff Result UI - Right")]
    public Image rightClaimImage;
    public Image rightClaimBorder;
    public Image rightActualActionImage;
    public Image rightActualActionBorder;
    public Image rightCallImage;
    public Image rightCallCheckImage;
    
    [Header("Log Text Box")]
    public GameObject logBox;
    public TextMeshProUGUI logText;
    
    [Header("Combat VFX - Start / Crit / Miss")]
    public GameObject startFightPrefab;
    public GameObject critVfxPrefab;        // PLACEHOLDER: assign your Crit image prefab
    public GameObject missVfxPrefab;        // PLACEHOLDER: assign your Miss image prefab
    public RectTransform startFightAnchor; 
    public RectTransform critVfxAnchor;     // Center of the canvas
    public RectTransform leftMissAnchor;    // Pre-placed on the left side of the canvas
    public RectTransform rightMissAnchor;   // Pre-placed on the right side of the canvas
    
    [SerializeField] private string[] attackDialogues =
    {
        "WOLOLO",
        "Kept you waiting huh",
        "Bomb Venezuela",
        "Why are we fighting again?"
    };
    
    [Header("Dialogue Frog Images")]

    // Left frog parts
    public Image leftBody;
    public Image leftHead;
    public Image leftEyes;
    public Image leftMouth;
    public Image leftHat;

    // Right frog parts
    public Image rightBody;
    public Image rightHead;
    public Image rightEyes;
    public Image rightMouth;
    public Image rightHat;

    [Header("Buff / Debuff Icons - Left Agent")]
    public GameObject leftAttackBuffIcon;
    public GameObject leftAttackDebuffIcon;
    public GameObject leftDefenseBuffIcon;
    public GameObject leftDefenseDebuffIcon;

    [Header("Buff / Debuff Icons - Right Agent")]
    public GameObject rightAttackBuffIcon;
    public GameObject rightAttackDebuffIcon;
    public GameObject rightDefenseBuffIcon;
    public GameObject rightDefenseDebuffIcon;

    private Coroutine leftHealthRoutine;
    private Coroutine rightHealthRoutine;
    private readonly List<Button> activeAttackButtons = new();

    [Header("Emotion Selection")] 
    public TMP_Dropdown emotionDropdown;
    public TextMeshProUGUI leftEmotionText;
    public TextMeshProUGUI rightEmotionText;
    public TextMeshProUGUI leftFocusScoreText;
    public TextMeshProUGUI rightFocusScoreText;
    
    [Header("Money Bars")]
    public Slider leftMoneyBar;
    public Slider rightMoneyBar;

    [Header("Money Bar Trackers")]
    public Image leftMoneyTracker;
    public Image rightMoneyTracker;

    [Header("Money Tracker Sprites")]
    public Sprite lowMoneySprite;
    public Sprite midMoneySprite;
    public Sprite highMoneySprite;

    private string _pendingAction;
    private string _pendingSignal;
    private string _pendingCallGuess;
    private string _pendingBanter;

    private static readonly string[] BanterDefend = {
        "HA! You did no damage to me with my shield on!",
        "My defense is impenetrable, try harder!",
        "Is that all you've got? My shield laughs at you!",
        "Splash against my shell all you want, I'm not moving!",
        "You can't crack this lily pad fortress!"
    };

    private static readonly string[] BanterSpecial = {
        "I'm charging my Geyser — prepare to be destroyed!",
        "Feel the pressure building... your end is coming!",
        "The Geyser awakens — there's no stopping it now!",
        "I'm unleashing everything I've got next turn!",
        "You can't outrun what's already beneath you..."
    };

    private static readonly string[] BanterGeneric = {
        "You won't see this coming!",
        "Your move, challenger.",
        "The pond grows still before the storm...",
        "Every ripple has a consequence, remember that.",
        "I've been waiting for this moment all match!"
    };

    private const int MaxFp = 8;

     private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialogueGroup)
            dialogueGroup.alpha = 0;

        if (logBox)
            logBox.SetActive(false);
        
        if (emotionDropdown)
        {
            emotionDropdown.ClearOptions();
            List<string> emotions = new List<string> {
                "Triumphant", "Optimistic", "Content",
                "Angry", "Annoyed", "Downcast",
                "Assured", "Steady", "Neutral",
                "Defeated", "Dismayed", "Apathetic"
            };
            emotionDropdown.AddOptions(emotions);
            // Default to Neutral (index 8)
            emotionDropdown.value = 8; 
        }
    }

    // ========= Helper to know left/right side =========

    private bool IsLeftAgent(AgentRunTime agent)
    {
        if (GameManager.Instance && GameManager.Instance.LeftAgent)
            return agent == GameManager.Instance.LeftAgent;

        return agent.transform.position.x < 0f;
    }

    // ========= Attack Buttons =========

    public void ShowAttackButtons(AgentRunTime agent)
    {
        // Hide both panels first
        if (AttackPanel) AttackPanel.SetActive(false);
        if (CallOrClaimPanel) CallOrClaimPanel.SetActive(false);

        // Keep attackPanel reference for InstantiateButton — points to active panel
        bool isLeft = IsLeftAgent(agent);

        if (isLeft)
        {
            if (fpLeftText)
                fpLeftText.text = $"FP: {agent.fpLeft}/{MaxFp}";
            UpdateFPLights(agent.fpLeft, true);
        }
        else
        {
            if (fpRightText)
                fpRightText.text = $"FP: {agent.fpLeft}/{MaxFp}";
            UpdateFPLights(agent.fpLeft, false);
        }

        ClearButtons(activeAttackButtons);
        ShowCallPhase(agent, onCallDone: () => HideClaimAndActualImages());
    }

    public void SyncFPButtons(int fpLeft)
    {
        if (activeAttackButtons.Count < 3) return;

        bool canPrepare = fpLeft > 0;

        if (activeAttackButtons[1])
        {
            activeAttackButtons[1].interactable = canPrepare;
            SetButtonColors(activeAttackButtons[1], canPrepare ? Color.white : Color.grey);
        }

        if (activeAttackButtons[2])
        {
            activeAttackButtons[2].interactable = canPrepare;
            SetButtonColors(activeAttackButtons[2], canPrepare ? Color.white : Color.grey);
        }

        if (fpLeftText)
            fpLeftText.text = $"FP: {fpLeft}/{MaxFp}";

        UpdateFPLights(fpLeft, true);
    }
    
    private void UpdateFPLights(int fp, bool isLeft)
    {
        GameObject[] lights = isLeft ? frogPointsLeft : frogPointsRight;

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i])
            {
                lights[i].SetActive(i < fp);
            }
        }
    }

    // Called by ActionManager to update the opponent's FP lights (display only, no button logic)
    public void UpdateOpponentFPLights(int fp, bool opponentIsLeft)
    {
        UpdateFPLights(fp, opponentIsLeft);

        // Also update the text if it exists
        if (opponentIsLeft)
        {
            if (fpLeftText) fpLeftText.text = $"FP: {fp}/{MaxFp}";
        }
        else
        {
            if (fpRightText) fpRightText.text = $"FP: {fp}/{MaxFp}";
        }
    }
    private void ShowAttackPhase(AgentRunTime agent)
    {
        if (CallOrClaimPanel) CallOrClaimPanel.SetActive(false);
        if (AttackPanel) AttackPanel.SetActive(true);
        attackPanel = AttackPanel;

        ClearButtons(activeAttackButtons);

        InstantiateButton(fastAttackButtonPrefab, () => OnAttackSelected(0, agent));

        if (agent.fpLeft > 0)
        {
            InstantiateButton(geyserButtonPrefab, () => OnAttackSelected(1, agent));
            InstantiateButton(defendButtonPrefab, () => OnAttackSelected(2, agent));
        }

        SetButtonsInteractable(true);
    }

    private void OnAttackSelected(int index, AgentRunTime agent)
    {
        _pendingAction = (index == 0) ? "FastAttack" : (index == 1) ? "SpecialAttack" : "Defend";
        _pendingSignal = null;

        // FastAttack has no claim — submit immediately
        if (_pendingAction == "FastAttack")
        {
            SubmitMove();
        }
        else
        {
            ShowClaimPhase(agent);
        }
    }

    private void ShowClaimPhase(AgentRunTime agent)
    {
        if (AttackPanel) AttackPanel.SetActive(false);
        if (CallOrClaimPanel) CallOrClaimPanel.SetActive(true);
        if (callOrClaimLabel) callOrClaimLabel.text = "Claim";
        attackPanel = CallOrClaimPanel;

        ClearButtons(activeAttackButtons);

        InstantiateButton(claimSpecialButtonPrefab, () =>
        {
            _pendingSignal = "SigSpecial";
            SubmitMove();
        });

        InstantiateButton(claimDefenseButtonPrefab, () =>
        {
            _pendingSignal = "SigDefend";
            SubmitMove();
        });
    }

    private void ShowCallPhase(AgentRunTime playerAgent, System.Action onCallDone = null, bool skipAttackPhase = false)
    {
        bool playerIsLeft = IsLeftAgent(playerAgent);
        AgentRunTime opponent = playerIsLeft
            ? GameManager.Instance.RightAgent
            : GameManager.Instance.LeftAgent;

        // No signal — skip call phase entirely
        if (string.IsNullOrEmpty(opponent.lastSignal))
        {
            _pendingCallGuess = null;
            onCallDone?.Invoke();
            if (!skipAttackPhase) ShowAttackPhase(playerAgent);
            return;
        }

        if (AttackPanel) AttackPanel.SetActive(false);
        if (CallOrClaimPanel) CallOrClaimPanel.SetActive(true);
        if (callOrClaimLabel) callOrClaimLabel.text = "Call Bluff?";
        attackPanel = CallOrClaimPanel;

        ClearButtons(activeAttackButtons);

        Button callBtn = InstantiateButton(callBluffButtonPrefab, () => {
            if (opponent.lastSignal == "SigDefend") _pendingCallGuess = "GuessSpecial";
            else if (opponent.lastSignal == "SigSpecial") _pendingCallGuess = "GuessDefend";
            onCallDone?.Invoke();
            if (!skipAttackPhase) ShowAttackPhase(playerAgent);
        });

        callBtn.interactable = true;
        SetButtonColors(callBtn, Color.white);

        InstantiateButton(noCallButtonPrefab, () =>
        {
            _pendingCallGuess = null;
            onCallDone?.Invoke();
            if (!skipAttackPhase) ShowAttackPhase(playerAgent);
        });
    }

    private void SubmitMove()
    {
        string selectedEmotion = "Neutral";

        if (emotionDropdown && emotionDropdown.options.Count > 0 && emotionDropdown.gameObject.activeSelf)
        {
            selectedEmotion = emotionDropdown.options[emotionDropdown.value].text;
        }

        // Pick banter based on what the player is CLAIMING (pendingSignal), not what they actually did.
        // This is the bluff — your banter matches your claim, not your true action.
        if (_pendingSignal == "SigSpecial")
            _pendingBanter = BanterSpecial[Random.Range(0, BanterSpecial.Length)];
        else if (_pendingSignal == "SigDefend")
            _pendingBanter = BanterDefend[Random.Range(0, BanterDefend.Length)];
        else
            _pendingBanter = BanterGeneric[Random.Range(0, BanterGeneric.Length)];

        PlayerMovePayload payload = new PlayerMovePayload
        {
            action           = _pendingAction,
            emotional_state  = selectedEmotion,
            preparing_signal = _pendingSignal,
            call_guess       = _pendingCallGuess
        };
        
        Debug.Log($"[UIManager] Submitting: {_pendingAction} | Sig: {_pendingSignal} | Call: {_pendingCallGuess}");
        JSBridge.SendAction(payload);
        
        SetButtonsInteractable(false);
        ToggleActionPanel(false);
    }

    // Called by ActionManager when agents[playerIndex].charging == true.
    // Per spec: on the Special resolve round the player has no action choice.
    // BUT the player can still Call before we auto-submit.
    public void AutoSubmitChargingMove()
    {
        AgentRunTime myAgent = (ActionManager.Instance.CurrentPlayerAssigned == 0)
            ? GameManager.Instance.LeftAgent
            : GameManager.Instance.RightAgent;

        // Show CallOrClaimPanel for the Call phase
        if (AttackPanel) AttackPanel.SetActive(false);
        if (CallOrClaimPanel) CallOrClaimPanel.SetActive(true);
        if (callOrClaimLabel) callOrClaimLabel.text = "Call Bluff?";
        attackPanel = CallOrClaimPanel;

        ClearButtons(activeAttackButtons);

        // Show Call phase — after player decides, auto-submit with action=null
        ShowCallPhase(myAgent, onCallDone: () =>
        {
            SetButtonsInteractable(false);
            ToggleActionPanel(false);

            PlayerMovePayload payload = new PlayerMovePayload
            {
                action           = null,
                emotional_state  = "Neutral",
                preparing_signal = null,
                call_guess       = _pendingCallGuess
            };

            Debug.Log($"[UIManager] Auto-submitting charging resolve (action=null) | Call: {_pendingCallGuess}");
            JSBridge.SendAction(payload);
        }, skipAttackPhase: true);
    }

    private Button InstantiateButton(Button prefab, UnityEngine.Events.UnityAction onClickAction)
    {
        if (!prefab) return null;
        Button btn = Instantiate(prefab, attackPanel.transform);
        btn.gameObject.SetActive(true);
        btn.interactable = true;
        SetButtonColors(btn, Color.white);
        btn.onClick.AddListener(onClickAction);
        activeAttackButtons.Add(btn);
        return btn;
    }

    // Called by ActionManager to sync uses with backend
    public void SyncButtonStates(int specialUsesLeft, int maxSpecialUses, int defendUsesLeft, int maxDefendUses)
    {
        if (activeAttackButtons.Count < 3) return;
        
        var firstBtnText = activeAttackButtons[0].GetComponentInChildren<TextMeshProUGUI>().text;
        if (firstBtnText != "Fast Attack") return;
        
        // Index 1 is Special Attack
        if (activeAttackButtons[1])
        {
            var specialText = activeAttackButtons[1].GetComponentInChildren<TextMeshProUGUI>();
            specialText.text = $"Special Attack ({specialUsesLeft}/{maxSpecialUses})";
            
            bool canUseSpecial = specialUsesLeft > 0;
            activeAttackButtons[1].interactable = canUseSpecial;
            SetButtonColors(activeAttackButtons[1], canUseSpecial ? Color.white : Color.grey);
            //specialText.color = canUseSpecial ? Color.black : Color.gray;
        }

        // Index 2 is Defend
        if (activeAttackButtons[2])
        {
            var defendText = activeAttackButtons[2].GetComponentInChildren<TextMeshProUGUI>();
            defendText.text = $"Defend ({defendUsesLeft}/{maxDefendUses})";
            
            bool canUseDefend = defendUsesLeft > 0;
            activeAttackButtons[2].interactable = canUseDefend;
            SetButtonColors(activeAttackButtons[2], canUseDefend ? Color.white : Color.grey);
            //defendText.color = canUseDefend ? Color.black : Color.gray;
        }
    }
    
    public void ToggleActionPanel(bool isVisible)
    {
        if (AttackPanel) AttackPanel.SetActive(isVisible ? AttackPanel == attackPanel : false);
        if (CallOrClaimPanel) CallOrClaimPanel.SetActive(isVisible ? CallOrClaimPanel == attackPanel : false);
    }
    
    // ========= Health Bars =========

    public void InitializeHealth(AgentRunTime left, AgentRunTime right)
    {
        if (left && leftHealthBar)
        {
            leftHealthBar.maxValue = left.maxHealth;
            leftHealthBar.value = left.currentHealth;
        }

        if (right && rightHealthBar)
        {
            rightHealthBar.maxValue = right.maxHealth;
            rightHealthBar.value = right.currentHealth;
        }
        
        if (left && leftHealthText)
            leftHealthText.text = $"{(float)left.currentHealth / left.maxHealth * 100f:0}%";

        if (right && rightHealthText)
            rightHealthText.text = $">{(float)right.currentHealth / right.maxHealth * 100f:0}%";
    }

    public void UpdateHealthBars(AgentRunTime left, AgentRunTime right)
    {
        if (left)
        {
            UpdateSingleHealthBar(leftHealthBar, left.currentHealth, left.maxHealth, ref leftHealthRoutine);
            UpdateHealthText(leftHealthText, left.currentHealth, left.maxHealth);
        }

        if (right)
        {
            UpdateSingleHealthBar(rightHealthBar, right.currentHealth, right.maxHealth, ref rightHealthRoutine);
            UpdateHealthText(rightHealthText, right.currentHealth, right.maxHealth, fogOfWar: true);
        }
    }

    private void UpdateSingleHealthBar(Slider slider, int targetHealth, int maxHealth, ref Coroutine routine)
    {
        if (!slider) return;

        slider.maxValue = maxHealth;
        
        UpdateHealthBarColor(slider, targetHealth, maxHealth);

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(AnimateHealthBar(slider, slider.value, targetHealth, maxHealth));
    }
    
    private void UpdateHealthText(TextMeshProUGUI text, int current, int max, bool fogOfWar = false)
    {
        if (!text) return;

        float percent = (float)current / max * 100f;
        text.text = fogOfWar ? $">{percent:0}%" : $"{percent:0}%";
    }
    
    private void UpdateHealthBarColor(Slider slider, float current, float max)
    {
        if (!slider) return;

        float percentage = current / max;

        Image fill = slider.fillRect.GetComponent<Image>();
        if (!fill) return;

        if (percentage <= 0.05f)
            fill.color = Color.red;
        else if (percentage <= 0.25f)
            fill.color = new Color(1f, 0.5f, 0f); // Orange
        else if (percentage <= 0.5f)
            fill.color = Color.yellow;
        else
            fill.color = Color.white;
    }

    private IEnumerator AnimateHealthBar(Slider slider, float startValue, float targetValue, float maxValue)
    {
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            slider.value = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }

        slider.value = targetValue;
    }

    // ========= Dialogue =========

    public void ShowDialogueLine(AgentRunTime agent, string customText = null)
    {
       
        if (nameText)
            nameText.text = agent.DisplayName;

        if (!string.IsNullOrEmpty(customText))
        {
            dialogueText.text = customText;
        }
        else
        {
            int randomIndex = Random.Range(0, attackDialogues.Length);
            dialogueText.text = attackDialogues[randomIndex];
        }
        
        bool isLeft = IsLeftAgent(agent);
        // Rotate the bubble (CanvasGroup parent)
        dialogueGroup.transform.localRotation = isLeft
            ? Quaternion.Euler(0, 180, 0)
            : Quaternion.Euler(0, 0, 0);

        // Counter-rotate text so it stays readable
        dialogueText.rectTransform.localRotation = isLeft
            ? Quaternion.Euler(0, 180, 0)
            : Quaternion.Euler(0, 0, 0);

        // Also fix the speaker name (same issue)
        nameText.rectTransform.localRotation = isLeft
            ? Quaternion.Euler(0, 180, 0)
            : Quaternion.Euler(0, 0, 0);

        /*SetFrogAlpha(true, isLeft);
        SetFrogAlpha(false, !isLeft);*/

        //StopAllCoroutines();
        StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        if (!dialogueGroup) yield break;

        // Fade in
        dialogueGroup.alpha = 0;
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            dialogueGroup.alpha = t / 0.5f;
            yield return null;
        }
        dialogueGroup.alpha = 1f;

        yield return new WaitForSeconds(2f);

        // Fade out
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            dialogueGroup.alpha = 1f - (t / 0.5f);
            yield return null;
        }
        dialogueGroup.alpha = 0f;
    }

    // ========= Turn Indicator =========

    public void ShowPlayerNames()
    {
        if (GameManager.Instance.LeftAgent)
        {
            leftTurnText.text = $"#{GameManager.Instance.LeftAgent.nftId}";
            leftTurnText.gameObject.SetActive(true);
        }

        if (GameManager.Instance.RightAgent)
        {
            rightTurnText.text = $"#{GameManager.Instance.RightAgent.nftId}";
            rightTurnText.gameObject.SetActive(true);
        }
    }

    public void UpdateRoundNumber(int round)
    {
        if (roundText) roundText.text = round.ToString("D2");
    }
    
    // ========= Buff / Debuff Icons =========

    public void UpdateBuffDebuffIcons(AgentRunTime agent)
    {
        // Leaving this intact in case you implement backend buff logic later.
        if (!agent) return;
        
        bool leftSide = IsLeftAgent(agent);

        GameObject attackBuff = leftSide ? leftAttackBuffIcon : rightAttackBuffIcon;
        GameObject attackDebuff = leftSide ? leftAttackDebuffIcon : rightAttackDebuffIcon;
        GameObject defenseBuff = leftSide ? leftDefenseBuffIcon : rightDefenseBuffIcon;
        GameObject defenseDebuff = leftSide ? leftDefenseDebuffIcon : rightDefenseDebuffIcon;

        attackBuff.SetActive(false);
        attackDebuff.SetActive(false);
        defenseBuff.SetActive(false);
        defenseDebuff.SetActive(false);

        /* Note: ActionManager/AgentRunTime no longer track modifier floats automatically, 
           but this logic safely does nothing until you hook it up to GDD backend events. */
    }

    // ========= Utility =========

    public void SetButtonsInteractable(bool interactable)
    {
        foreach (var btn in activeAttackButtons)
            if (btn) btn.interactable = interactable;
    }

    private void SetButtonColors(Button btn, Color color)
    {
        ColorBlock colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color;
        colors.pressedColor = color;
        colors.disabledColor = Color.grey;
        btn.colors = colors;
    }

    private void ClearButtons(List<Button> buttons)
    {
        foreach (var b in buttons)
            if (b) Destroy(b.gameObject);
        buttons.Clear();
    }
    
    public void HideAllPlayerUI()
    {
        if (AttackPanel) AttackPanel.SetActive(false);
        if (CallOrClaimPanel) CallOrClaimPanel.SetActive(false);
    }

    public void EnableAllPlayerUI()
    {
        // Player vs Player — no special control
    }

    public void EnablePlayerVsAIUI()
    {
        // Player is always left side in PvE
    }

    public void UpdatePlayerNames(string walletAddress, bool isLeftPlayer)
    {
        Debug.Log($"Player name is {walletAddress}");
    }
    
    public void UpdateEmotionDisplays(string leftEmotion, string rightEmotion)
    {
        if (leftEmotionText) leftEmotionText.text = $"State: {leftEmotion}";
        if (rightEmotionText) rightEmotionText.text = $"State: {rightEmotion}";
    }

    public void UpdateFocusScores(int leftScore, int rightScore)
    {
        if (leftFocusScoreText) leftFocusScoreText.text = $"Focus: {leftScore}";
        if (rightFocusScoreText) rightFocusScoreText.text = $"Focus: {rightScore}";
    }
    
    
    // ========= Combat VFX =========

    // Task.Delay and Task.Yield are unreliable in WebGL (blocked by AudioContext init).
    // We use coroutines for all timing and only use TaskCompletionSource as a signal.
    public Task ShowStartFight()
    {
        if (startFightPrefab == null || startFightAnchor == null)
            return Task.CompletedTask;

        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        ActionManager.Instance.RegisterIntroTask(tcs.Task);

        GameObject vfx = Instantiate(startFightPrefab, startFightAnchor);
        RectTransform rt = vfx.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        // Coroutine handles all timing — reliable in WebGL
        StartCoroutine(IntroSequence(vfx, tcs));
        return tcs.Task;
    }

    private IEnumerator IntroSequence(GameObject vfx, TaskCompletionSource<bool> tcs)
    {
        // Hold for 2 seconds
        yield return new WaitForSeconds(2f);

        // Fade out over 2 seconds
        CanvasGroup cg = vfx != null ? vfx.GetComponent<CanvasGroup>() : null;
        Image img      = vfx != null ? vfx.GetComponent<Image>() : null;

        float elapsed = 0f;
        float duration = 2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            if (cg != null)  cg.alpha = alpha;
            else if (img != null) img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            yield return null;
        }

        if (vfx != null) Destroy(vfx);

        // Signal ActionManager that the intro is fully gone — queue now unblocks
        tcs.SetResult(true);
    }
    
    // Call this for "Crit" and "CritBlocked" — spawns at the center anchor
    public void ShowCritVFX()
    {
        if (critVfxPrefab == null || critVfxAnchor == null) return;

        GameObject vfx = Instantiate(critVfxPrefab, critVfxAnchor);
        BackgroundManager.Instance.PlayCrowdAmbience();
        // Reset so it sits exactly at the anchor position
        RectTransform rt = vfx.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
        StartCoroutine(FadeAndDestroy(vfx, 2f));
    }

    // Call this for "Miss" — spawns at the defender's side anchor
    public void ShowMissVFX(bool defenderIsLeft)
    {
        if (missVfxPrefab == null) return;

        RectTransform anchor = defenderIsLeft ? leftMissAnchor : rightMissAnchor;
        if (anchor == null) return;

        GameObject vfx = Instantiate(missVfxPrefab, anchor);
        RectTransform rt = vfx.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
        StartCoroutine(FadeAndDestroy(vfx, 1.5f));
    }

    // Fades out a CanvasGroup (or Image alpha) over `duration` then destroys the object
    private IEnumerator FadeAndDestroy(GameObject vfx, float duration)
    {
        if (!vfx) yield break;

        // Prefer CanvasGroup for clean fade; fall back to Image alpha
        CanvasGroup cg = vfx.GetComponent<CanvasGroup>();
        Image img      = vfx.GetComponent<Image>();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            if (cg)  cg.alpha  = alpha;
            else if (img) img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);

            yield return null;
        }

        Destroy(vfx);
    }
    
    // ========= Confidence/Money Bar =========
    private void UpdateMoneySprite(Slider bar, Image tracker)
    {
        // Safety check to prevent dividing by zero
        if (!bar || !tracker || bar.maxValue == 0) return;

        // Calculate how full the bar is (0.0 to 1.0)
        float percent = bar.value / bar.maxValue;

        // Swap the sprite based on the percentage
        if (percent <= 0.25f)
            tracker.sprite = lowMoneySprite;
        else if (percent <= 0.75f)
            tracker.sprite = midMoneySprite;
        else
            tracker.sprite = highMoneySprite;
    }

    // ========= Bluff Result UI =========

    // Called at start of round sequence.
    // Left (player): Claim + ActualAction + truth/lie border
    // Right (agent): Claim + ? (border hidden until reveal)
    public void ShowClaimAndActualImages(RoundResult round)
    {
        // ── PLAYER (left) ──
        string playerSignal = round.moves != null && round.moves.Count > 0 ? round.moves[0]?.preparing_signal : null;
        string playerAction = round.actions != null && round.actions.Count > 0 ? round.actions[0] : null;

        if (!string.IsNullOrEmpty(playerSignal))
        {
            // Claim: show icon + truth/lie border (claim matches actual = truth, else lie)
            ShowImageNow(leftClaimImage, SignalToSprite(playerSignal));
            bool playerToldTruth = ClaimMatchesAction(playerSignal, playerAction);
            ShowImageNow(leftClaimBorder, playerToldTruth ? truthBorderSprite : lieBorderSprite);

            // Actual: show icon + neutral border
            ShowImageNow(leftActualActionImage, ActionToSprite(playerAction));
            ShowImageNow(leftActualActionBorder, neutralActualBorderSprite);
        }
        else if (playerAction == "Resolve")
        {
            // Geyser resolve — show geyser actual + neutral border, no claim
            HideImage(leftClaimImage);
            HideImage(leftClaimBorder);
            ShowImageNow(leftActualActionImage, signalSpecialSprite);
            ShowImageNow(leftActualActionBorder, neutralActualBorderSprite);
        }
        else
        {
            // FastAttack — no claim, no actual
            HideImage(leftClaimImage);
            HideImage(leftClaimBorder);
            HideImage(leftActualActionImage);
            HideImage(leftActualActionBorder);
        }

        // ── AGENT (right) ──
        string agentSignal = round.moves != null && round.moves.Count > 1 ? round.moves[1]?.preparing_signal : null;

        if (!string.IsNullOrEmpty(agentSignal))
        {
            // Claim: show icon + neutral border (we don't know truth/lie until call)
            ShowImageNow(rightClaimImage, SignalToSprite(agentSignal));
            ShowImageNow(rightClaimBorder, neutralBorderSprite);

            // Actual: show ? + neutral border (reveals on call)
            ShowImageNow(rightActualActionImage, unknownSprite);
            ShowImageNow(rightActualActionBorder, neutralActualBorderSprite);
        }
        else
        {
            // No claim — hide all
            HideImage(rightClaimImage);
            HideImage(rightClaimBorder);
            HideImage(rightActualActionImage);
            HideImage(rightActualActionBorder);
        }
    }

    // Flips agent ? to actual action + sets border. Called inside ShowCallSequence.
    // Also handles agent's call result on the right side.
    private void RevealAgentAction(RoundResult round)
    {
        bool? callSuccess = round.call_success != null && round.call_success.Count > 0 ? round.call_success[0] : (bool?)null;
        string callGuess  = round.moves != null && round.moves.Count > 0 ? round.moves[0]?.call_guess : null;

        if (callSuccess == null || string.IsNullOrEmpty(callGuess))
            return; // no call — keep ?

        Sprite revealedSprite;
        bool agentToldTruth;

        if (callSuccess.Value)
        {
            // Call succeeded → agent lied
            revealedSprite = callGuess == "GuessSpecial" ? signalSpecialSprite : signalDefendSprite;
            agentToldTruth = false;
        }
        else
        {
            // Call failed → agent told truth
            revealedSprite = callGuess == "GuessSpecial" ? signalDefendSprite : signalSpecialSprite;
            agentToldTruth = true;
        }

        ShowImageNow(rightActualActionImage, revealedSprite);
        ShowImageNow(rightActualActionBorder, neutralActualBorderSprite);
        ShowImageNow(rightClaimBorder, agentToldTruth ? truthBorderSprite : lieBorderSprite);
    }

    // Also handles agent's call result on the right side.
    private void RevealAgentCallResult(RoundResult round)
    {
        bool? agentCallSuccess = round.call_success != null && round.call_success.Count > 1 ? round.call_success[1] : (bool?)null;
        if (agentCallSuccess == null) return;

        ShowImageNow(rightCallImage, callSprite);
        ShowImageNow(rightCallCheckImage, agentCallSuccess.Value ? checkSprite : crossSprite);
    }

    // Shows CALL → delay → Check/X + reveal agent action. Awaited by ActionManager.
    public IEnumerator ShowCallSequence(RoundResult round)
    {
        bool? leftCallSuccess  = round.call_success != null && round.call_success.Count > 0
            ? round.call_success[0]
            : (bool?)null;

        bool? rightCallSuccess = round.call_success != null && round.call_success.Count > 1
            ? round.call_success[1]
            : (bool?)null;

        bool anyCall = leftCallSuccess != null || rightCallSuccess != null;
        if (!anyCall)
        {
            HideLogText();
            yield break;
        }

        AgentRunTime leftAgent = GameManager.Instance.LeftAgent;
        AgentRunTime rightAgent = GameManager.Instance.RightAgent;

        HideLogText();

        // Show CALL icons first
        if (leftCallSuccess != null)
            ShowImageNow(leftCallImage, callSprite);

        if (rightCallSuccess != null)
            ShowImageNow(rightCallImage, callSprite);

        yield return new WaitForSeconds(1f);

        if (leftCallSuccess != null)
        {
            ShowLogText(BuildCallResultText(true, leftCallSuccess.Value));
            ShowImageNow(leftCallCheckImage, leftCallSuccess.Value ? checkSprite : crossSprite);

            if (leftCallSuccess.Value && leftAgent)
                leftAgent.PlayBuffEffect();

            RevealAgentAction(round);

            yield return new WaitForSeconds(4f);
            HideLogText();
            yield return new WaitForSeconds(0.25f);
        }

        if (rightCallSuccess != null)
        {
            ShowLogText(BuildCallResultText(false, rightCallSuccess.Value));
            ShowImageNow(rightCallCheckImage, rightCallSuccess.Value ? checkSprite : crossSprite);

            if (rightCallSuccess.Value && rightAgent)
                rightAgent.PlayBuffEffect();

            yield return new WaitForSeconds(4f);
        }

        yield return FadeOutImages(
            new[] { leftCallImage, leftCallCheckImage, rightCallImage, rightCallCheckImage },
            0.5f
        );

        HideImage(leftCallImage);
        HideImage(leftCallCheckImage);
        HideImage(rightCallImage);
        HideImage(rightCallCheckImage);

        HideLogText();
    }

    // Called when Call phase of next turn ends — hides claim + actual action icons
    public void HideClaimAndActualImages()
    {
        HideImage(leftClaimImage);
        HideImage(leftClaimBorder);
        HideImage(leftActualActionImage);
        HideImage(leftActualActionBorder);
        HideImage(rightClaimImage);
        HideImage(rightClaimBorder);
        HideImage(rightActualActionImage);
        HideImage(rightActualActionBorder);
    }

    // ── Helpers ──

    private void ShowImageNow(Image image, Sprite sprite)
    {
        if (!image || !sprite) return;
        image.sprite = sprite;
        image.gameObject.SetActive(true);
        SetImageAlpha(image, 1f);
    }

    private void HideImage(Image image)
    {
        if (!image) return;
        SetImageAlpha(image, 0f);
        image.gameObject.SetActive(false);
    }

    private IEnumerator FadeOutImages(Image[] images, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            foreach (var img in images) SetImageAlpha(img, alpha);
            yield return null;
        }
    }

    private bool ClaimMatchesAction(string signal, string action)
    {
        if (signal == "SigDefend"  && action == "Defend")        return true;
        if (signal == "SigSpecial" && action == "SpecialAttack") return true;
        return false;
    }

    private Sprite SignalToSprite(string signal)
    {
        if (signal == "SigDefend")  return signalDefendSprite;
        if (signal == "SigSpecial") return signalSpecialSprite;
        return null;
    }

    private Sprite ActionToSprite(string action)
    {
        if (action == "Defend")        return signalDefendSprite;
        if (action == "SpecialAttack") return signalSpecialSprite;
        if (action == "Resolve")       return signalSpecialSprite; // geyser resolving
        return null;
    }

    public string ConsumePendingBanter()
    {
        string b = _pendingBanter;
        _pendingBanter = null;
        return b;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (!image) return;
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private void Update()
    {
        UpdateMoneySprite(leftMoneyBar, leftMoneyTracker);
        UpdateMoneySprite(rightMoneyBar, rightMoneyTracker);
    }
    
    public void ShowLogText(string message)
    {
        if (logText)
            logText.text = message;

        if (logBox)
            logBox.SetActive(true);
    }

    public void HideLogText()
    {
        if (logText)
            logText.text = string.Empty;

        if (logBox)
            logBox.SetActive(false);
    }

    private string BuildCallResultText(bool callerIsLeft, bool success)
    {
        string agentName = GameManager.Instance.RightAgent != null
            ? GameManager.Instance.RightAgent.DisplayName
            : "Agent";

        if (callerIsLeft)
        {
            return success
                ? $"Player successfully calls {agentName}'s bluff. PowerUp gained!"
                : $"Player's call fails, {agentName} was telling the truth! Loses 1 FP.";
        }
        else
        {
            return success
                ? $"{agentName} successfully calls Player's bluff. PowerUp gained!"
                : $"{agentName}'s call failed. Player was telling the truth! Loses 1 FP.";
        }
    }
    
    
}