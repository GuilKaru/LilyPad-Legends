using System;
using UnityEngine;

public class AgentRunTime : MonoBehaviour
{
    // Public Fields
    #region Public Fields
    
    [Header("Stats")]
    public AgentData data;
    
    [SerializeField] private int _currentHealth;
    public int currentHealth
    {
        get => _currentHealth;
        set => _currentHealth = value;
    }
    
    [Header("Protect Shield")]
    public GameObject shieldPrefab;
    public Transform shieldSpriteHolder;  
    [HideInInspector]
    public GameObject activeShield;
    
    [Header("Normal Attack")]
    public GameObject normalAttackVfxPrefab;
    public Transform normalAttackVfxHolder;
    
    [Header("Charge Attack")]
    public GameObject chargeVfxPrefab;
    public Transform chargeVfxHolder;

    [Header("Preparing Charge Attack")]
    public GameObject preparingChargeVfxPrefab;
    public Transform preparingChargeVfxHolder;
    
    [Header("Buff / Debuff Visuals")]
    public GameObject buffPrefab;
    public GameObject debuffPrefab;
    public GameObject healPrefab;

    [Header("Tactics & State")] 
    public int fpLeft; //Current Tactic Points
    public int nftId;    // NFT ID synced from backend

    public string lastSignal; // The signal this agent showed LAST turn

    [HideInInspector] public GameObject activeBuff;
    [HideInInspector] public GameObject activeDebuff;

    // Effects applied AFTER flicker
    [HideInInspector] public SelfEffects pendingEffects;

    [Header("Animation")]
    public AgentVisualController visualController;
    public Transform visualEffectsSpriteHolder;
    public Transform spriteHolder;
    public Transform headSpriteHolder;
    public Transform mouthSpriteHolder;
    public Transform eyesSpriteHolder;
    public Transform hatSpriteHolder;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioSource sfxSource;

    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip fastAttackClip;
    [SerializeField] private AudioClip prepareChargeClip;
    [SerializeField] private AudioClip releaseChargeClip;
    [SerializeField] private AudioClip shieldClip;
    [SerializeField] private AudioClip critClip;
    [SerializeField] private AudioClip notEffectiveDamageClip;
    [SerializeField] private AudioClip buffClip;
    
    #endregion

    // Unity Methods
    #region Unity Methods
    
    private void Awake()
    {
        visualController ??= GetComponentInChildren<AgentVisualController>();
        
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
    }
        
    #endregion

    // Initialization
    #region Initialization
    
    // We removed AttackDatabase since the backend handles all attack logic now.
    public void Initialize(AgentData agentData)
    {
        data = agentData;
        
        if (data != null && data.stats != null)
        {
            maxHealth = data.stats.maxHealth;
            currentHealth = data.stats.maxHealth;
            visualController.Initialize(data.visualId, data.visualHeadId, data.visualEyesId, data.visualMouthId, data.visualHatId);
            Debug.Log($"Initialized {data.name} | HP: {currentHealth}");
        }
        else
        {
            Debug.LogError("AgentData or AgentData.stats is null during initialization!");
        }
    }

    #endregion

    // Stats and States
    #region Stats and States
    
    [SerializeField] private int _maxHealth;
    public int maxHealth 
    { 
        get => _maxHealth; 
        set => _maxHealth = value; 
    }

    public string DisplayName => data != null ? data.name : "Unknown Agent";
    public bool IsAlive() => currentHealth > 0;
    
    #endregion
    
    // Visual Triggers (Called by ActionManager)
    #region Visual Triggers

    public void PlayIdleAnimation() => visualController.PlayIdle();
    
    public void PlayAttackAnimation() => visualController.PlayAttack();

    public void TriggerChargeVisuals()
    {
        visualController.PlayCharge();
        PlayChargeVFX();
    }

    public void TriggerFastAttackVisuals()
    {
        visualController.PlayAttack();
        PlayNormalVFX();
    }
    
    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (currentHealth == 0)
        {
            // Stop low HP flicker (if active)
            visualController.StopLowHpFlicker();

            // Play death animation
            visualController.PlayDeath();

            return; // IMPORTANT: avoid running damage flicker
        }

        // If still alive → continue normal hurt logic
        CheckLowHpState();
        visualController.PlayDamageFlicker();

    }
    
    public void CheckLowHpState()
    {
        float hpPercent = (float)currentHealth / maxHealth;

        if (hpPercent <= 0.25f)
            visualController.StartLowHpFlicker();
        else
            visualController.StopLowHpFlicker();
    }

    #endregion
    
    // VFX Instantiation
    #region VFX Instantiation
    
    public void PlayChargeVFX()
    {
        if (chargeVfxPrefab == null || chargeVfxHolder == null) return;
        GameObject vfx = Instantiate(chargeVfxPrefab, chargeVfxHolder, false);
        Destroy(vfx, 4f);
    }

    public void PlayNormalVFX()
    {
        if (normalAttackVfxPrefab == null || normalAttackVfxHolder == null) return;
        GameObject vfx = Instantiate(normalAttackVfxPrefab, normalAttackVfxHolder, false);
        Destroy(vfx, 1.25f);
    }

    public void PlayPreparingChargeVFX()
    {
        if (preparingChargeVfxPrefab == null || preparingChargeVfxHolder == null) return;
        GameObject vfx = Instantiate(preparingChargeVfxPrefab, preparingChargeVfxHolder, false);
        Destroy(vfx, 2f);
    }
    
    public void PlayHealEffect()
    {
        if (healPrefab == null || visualEffectsSpriteHolder == null) return;
        GameObject healFx = Instantiate(healPrefab, visualEffectsSpriteHolder.position, Quaternion.identity, visualEffectsSpriteHolder);
        Destroy(healFx, 0.6f); 
    }
    
    
    public void PlayBuffEffect()
    {
        PlayBuffSFX();
        if (buffPrefab == null || visualEffectsSpriteHolder == null) return;
        GameObject buffFx = Instantiate(buffPrefab, visualEffectsSpriteHolder.position, Quaternion.identity, visualEffectsSpriteHolder);
        Destroy(buffFx, 0.9f); 
    }
    
    public void ApplyVisualEffects(SelfEffects effects)
    {
        if (effects == null || visualEffectsSpriteHolder == null) return;

        bool positive = effects.attackModifier > 1f || effects.defenseModifier > 1f;
        bool negative = effects.attackModifier < 1f || effects.defenseModifier < 1f;

        // Positive → Buff
        if (positive)
        {
            if (activeDebuff) { Destroy(activeDebuff); activeDebuff = null; }
            if (buffPrefab)
            {
                activeBuff = Instantiate(buffPrefab, visualEffectsSpriteHolder.position, Quaternion.identity, visualEffectsSpriteHolder);
                Destroy(activeBuff, 0.6f); 
            }
        }

        // Negative → Debuff
        if (negative)
        {
            if (activeBuff) { Destroy(activeBuff); activeBuff = null; }
            if (debuffPrefab)
            {
                activeDebuff = Instantiate(debuffPrefab, visualEffectsSpriteHolder.position, Quaternion.identity, visualEffectsSpriteHolder);
                Destroy(activeDebuff, 0.6f);
            }
        }
    }
    
    public void TryApplyPendingEffectsImmediate()
    {
        if (pendingEffects != null)
        {
            ApplyVisualEffects(pendingEffects);
            pendingEffects = null;
        }
    }

    #endregion
    
    //Sound Effects

    #region SoundEffects

    public void PlayDamageSFX()
    {
        if (sfxSource != null && damageClip != null)
            sfxSource.PlayOneShot(damageClip);
    }

    public void PlayFastAttackSFX()
    {
        if (sfxSource != null && fastAttackClip != null)
            sfxSource.PlayOneShot(fastAttackClip);
    }

    public void PlayPrepareChargeSFX()
    {
        if (sfxSource != null && prepareChargeClip != null)
            sfxSource.PlayOneShot(prepareChargeClip);
    }

    public void PlayReleaseChargeSFX()
    {
        if (sfxSource != null && releaseChargeClip != null)
            sfxSource.PlayOneShot(releaseChargeClip);
    }

    public void PlayShieldSFX()
    {
        if (sfxSource != null && shieldClip != null)
            sfxSource.PlayOneShot(shieldClip);
    }

    public void PlayCritSFX()
    {
        if (sfxSource != null && critClip != null)
            sfxSource.PlayOneShot(critClip);
    }

    public void PlayNotEffectiveDamageSFX()
    {
        if (sfxSource != null && notEffectiveDamageClip != null)
            sfxSource.PlayOneShot(notEffectiveDamageClip);
    }
    
    public void PlayBuffSFX()
    {
        if (sfxSource != null && buffClip != null)
            sfxSource.PlayOneShot(buffClip);
    }


    #endregion
}