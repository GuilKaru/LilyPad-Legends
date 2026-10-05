using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance { get; private set; }

    private Queue<ServerMessage> _eventQueue = new Queue<ServerMessage>();
    private bool _isPlaying = false;
    private int _lastPlayedRound = 0;

    private int? _currentPlayerAssigned = null;
    public int? CurrentPlayerAssigned => _currentPlayerAssigned;
    private Task _introTask = null; // set by ShowStartFight; queue waits on it
    
    private bool _matchEnded = false;
    private bool _pendingChargingSubmit = false;
    
    public void RegisterIntroTask(Task t) => _introTask = t;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void EnqueueState(string jsonPayload)
    {
        ServerMessage envelope = JsonConvert.DeserializeObject<ServerMessage>(jsonPayload);
        
        // Only process actual state updates
        if (envelope == null || envelope.type != "state" || envelope.payload?.state == null) return;

        _eventQueue.Enqueue(envelope);
       

        if (!_isPlaying)
        {
            _isPlaying = true;
            _ = ProcessQueueAsync();
        }
    }

    private async Task ProcessQueueAsync()
    {
        // Wait for the intro to fully finish (registered by ShowStartFight) before touching any UI
        if (_introTask != null)
        {
            await _introTask;
            _introTask = null;
        }

        int? whoAmI = null;
        int? whoseTurnIsIt = null;
        
        while (_eventQueue.Count > 0)
        {
            ServerMessage currentMessage = _eventQueue.Dequeue();
            LilypadState state = currentMessage.payload.state;
            
            //Track the most recent permissions
            whoAmI = currentMessage.payload.you;
            whoseTurnIsIt = currentMessage.payload.current_player;
            
            if (whoAmI != null && _currentPlayerAssigned != whoAmI)
            {
                _currentPlayerAssigned = whoAmI;
                AgentRunTime myAgent = (whoAmI == 0) ? GameManager.Instance.LeftAgent : GameManager.Instance.RightAgent;
                UIManager.Instance.ShowAttackButtons(myAgent);
            }

            // Reset round tracker on new match
            if (state.round == 0) _lastPlayedRound = 0;

            // Update round number in UI
            if (state.round > 0) UIManager.Instance.UpdateRoundNumber(state.round);

            //Banter Phase
            if (state.phase == "Banter")
            {
                await PlayBanterPhase(state);
                SyncAgentData(state);
                SyncMatchVisuals(state, whoAmI ?? 0);
                continue;
            }
            
            //DesaturationManager.Instance.DeactivateDesaturation(); 
            SyncAgentData(state);
            
            // Play new rounds
            bool chargeRoundPlayed = false;
            if (state.round_history != null)
            {
                foreach (var round in state.round_history)
                {
                    if (round.round > _lastPlayedRound)
                    {
                        await PlayRoundSequence(round, state);
                        _lastPlayedRound = round.round;

                        // Charge round: stop here — resolve arrives as a separate state
                        if (round.actions != null && round.actions.Exists(a => a == "SpecialAttack"))
                        {
                            chargeRoundPlayed = true;
                            break;
                        }
                    }
                }
            }

            SyncMatchVisuals(state, whoAmI ?? 0);
            if (chargeRoundPlayed) break;
        }
        
        _isPlaying = false;

        // Submit charging null move NOW — queue is empty, _isPlaying = false.
        // Backend response will start a fresh ProcessQueueAsync run cleanly.
        if (_pendingChargingSubmit)
        {
            _pendingChargingSubmit = false;
            Debug.Log("[ActionManager] Submitting charging null move after queue cleared.");
            UIManager.Instance.AutoSubmitChargingMove();
            return;
        }

        if (_matchEnded) return;
        
        bool isMyTurn = (whoAmI != null) && (whoAmI == whoseTurnIsIt);
        UIManager.Instance.SetButtonsInteractable(isMyTurn);
        UIManager.Instance.ToggleActionPanel(isMyTurn);
        
        if (isMyTurn)
        {
            Debug.Log($"It is your turn (Player {whoAmI})! UI Unlocked.");
            AgentRunTime myAgent = (whoAmI == 0) ? GameManager.Instance.LeftAgent : GameManager.Instance.RightAgent;
            UIManager.Instance.ShowAttackButtons(myAgent);
        }
        else
        {
            Debug.Log($"Waiting for Player {whoseTurnIsIt} to act. UI Locked.");
        }
    }

    private Task PlayBanterPhase(LilypadState state)
    {
        // Banter phase = Rust collecting move submissions. Nothing to display here.
        // Banter is shown AFTER combat resolves, from round_history[n].banter in PlayRoundSequence.
        return Task.CompletedTask;
    }

    private async Task PlayRoundSequence(RoundResult round, LilypadState fullState)
    {
        AgentRunTime p1 = GameManager.Instance.LeftAgent;
        AgentRunTime p2 = GameManager.Instance.RightAgent;
        if (!p1 || !p2) return;

        // UPDATE FOCUS SCORES
        if (round.focus_score != null && round.focus_score.Count >= 2)
            UIManager.Instance.UpdateFocusScores(round.focus_score[0], round.focus_score[1]);

        // STEP 1 — Claim + ActualAction icons appear
        UIManager.Instance.ShowClaimAndActualImages(round);
        await WaitSeconds(1.5f);

        // STEP 2 — CALL label → Check/X + agent reveal (skipped if no call made)
        await WaitForCoroutine(UIManager.Instance.ShowCallSequence(round));

        // STEP 3 — Combat
        bool p1First = round.first_actor == 0;

        if (p1First)
        {
            await ExecuteAction(p1, p2,
                round.actions[0],
                Mathf.RoundToInt(round.damage_dealt[0]),
                round.attack_results[0]);

            if (p2.currentHealth > 0)
                await ExecuteAction(p2, p1,
                    round.actions[1],
                    Mathf.RoundToInt(round.damage_dealt[1]),
                    round.attack_results[1]);
        }
        else
        {
            await ExecuteAction(p2, p1,
                round.actions[1],
                Mathf.RoundToInt(round.damage_dealt[1]),
                round.attack_results[1]);

            if (p1.currentHealth > 0)
                await ExecuteAction(p1, p2,
                    round.actions[0],
                    Mathf.RoundToInt(round.damage_dealt[0]),
                    round.attack_results[0]);
        }

        if (p1.activeShield) { Destroy(p1.activeShield); p1.activeShield = null; }
        if (p2.activeShield) { Destroy(p2.activeShield); p2.activeShield = null; }

        // Banter
        // Claim + ActualAction icons stay visible through banter.
        // They are hidden in ShowAttackButtons when Call phase of next turn ends.
        await WaitSeconds(2f);
        if (_matchEnded) return;

        int playerIndex = _currentPlayerAssigned ?? 0;
        int agentIndex  = playerIndex == 0 ? 1 : 0;

        string playerBanter = UIManager.Instance.ConsumePendingBanter();
        string agentBanter  = round.banter != null && round.banter.Count > agentIndex ? round.banter[agentIndex] : null;

        AgentRunTime playerAgent = playerIndex == 0 ? p1 : p2;
        AgentRunTime agentAgent  = playerIndex == 0 ? p2 : p1;

        if (!string.IsNullOrWhiteSpace(playerBanter))
        {
            UIManager.Instance.ShowDialogueLine(playerAgent, playerBanter);
            await WaitSeconds(3f);
        }

        if (!string.IsNullOrWhiteSpace(agentBanter))
        {
            UIManager.Instance.ShowDialogueLine(agentAgent, agentBanter);
            await WaitSeconds(3f);
        }
    }

    private async Task ExecuteAction(AgentRunTime actor, AgentRunTime target, string action, int damage, string result)
    {
        bool actorIsPlayer = !(_currentPlayerAssigned.HasValue && (actor == GameManager.Instance.LeftAgent ? 0 : 1) != _currentPlayerAssigned.Value);

        if (action == "FastAttack")
        {
            if (actorIsPlayer) UIManager.Instance.ShowLogText("Player is using Fast Attack!");
            actor.TriggerFastAttackVisuals();
            actor.PlayFastAttackSFX();
            await WaitSeconds(0.8f);
            ApplyDamage(target, damage, result);
            if (_matchEnded) return;
            await WaitSeconds(1f);
            actor.visualController.PlayIdle();
            if (actorIsPlayer) UIManager.Instance.HideLogText();
            return;
        }

        if (action == "Prepare")
        {
            // Agent fog of war — backend sends "Prepare" for both Defend and SpecialAttack charge
            actor.visualController.PlayCharge();
            await WaitSeconds(1.5f);
            ApplyDamage(target, damage, result);
            return;
        }

        if (action == "Resolve")
        {
            // Special Attack resolves — both player and agent do this
            if (actorIsPlayer) UIManager.Instance.ShowLogText("Player is Executing Geyser!");
            actor.visualController.PlayIdle();
            actor.PlayChargeVFX();
            actor.PlayReleaseChargeSFX();
            await WaitSeconds(2.5f);
            ApplyDamage(target, damage, result);
            if (_matchEnded) return;
            await WaitSeconds(1f);
            if (actorIsPlayer) UIManager.Instance.HideLogText();
            return;
        }

        if (action == "SpecialAttack")
        {
            // Player charging Special Attack
            UIManager.Instance.ShowLogText("Player is Charging Geyser!");
            actor.visualController.PlayPreparingCharge();
            actor.PlayPrepareChargeSFX();
            await WaitSeconds(2f);
            actor.visualController.PlayIdle();
            UIManager.Instance.HideLogText();
            return;
        }

        if (action == "Defend")
        {
            // Player defending
            UIManager.Instance.ShowLogText("Player is Defending!");
            PlayDefendVisual(actor);
            actor.PlayShieldSFX();
            await WaitSeconds(2.5f);
            UIManager.Instance.HideLogText();
            return;
        }
    }

    private void PlayDefendVisual(AgentRunTime defender)
    {
        if (defender.shieldPrefab && !defender.activeShield)
            defender.activeShield = Instantiate(defender.shieldPrefab, defender.shieldSpriteHolder.position, Quaternion.identity, defender.shieldSpriteHolder);
    }

    private void ApplyDamage(AgentRunTime target, int damage, string result)
    {
        bool targetIsLeft = target == GameManager.Instance.LeftAgent;

        // MISS: show on defender side, no damage, no flicker
        if (result == "Miss")
        {
            UIManager.Instance.ShowMissVFX(targetIsLeft);
            return;
        }

        // CRIT or CRIT_BLOCKED: show at center before anything else
        if (result == "Crit" || result == "CritBlocked")
        {
            UIManager.Instance.ShowCritVFX();
            target.PlayCritSFX();
        }

        // CRIT_BLOCKED: defender fully blocked — no damage, no flicker
        if (result == "CritBlocked")
        {
            target.PlayNotEffectiveDamageSFX();
            if (target.activeShield) { Destroy(target.activeShield); target.activeShield = null; }
            return;
        }

        target.currentHealth = Mathf.Max(0, target.currentHealth - damage);

        if (target.activeShield)
        {
            target.PlayNotEffectiveDamageSFX();
            Destroy(target.activeShield);
            target.activeShield = null;
            // Shield absorbed the hit — no flicker
        }
        else
        {
            target.PlayDamageSFX();
            target.CheckLowHpState();
            target.visualController.PlayDamageFlicker();
        }


        UIManager.Instance.UpdateHealthBars(
            GameManager.Instance.LeftAgent,
            GameManager.Instance.RightAgent);
        
        if (target.currentHealth <= 0)
        {
            BackgroundManager.Instance.PlayCrowdAmbience();
            EndMatch(target);
        }
    }

    private void SyncAgentData(LilypadState state)
    {
        AgentRunTime p1 = GameManager.Instance.LeftAgent;
        AgentRunTime p2 = GameManager.Instance.RightAgent;

        if (p1 && state.agents != null && state.agents.Count > 0)
        {
            p1.fpLeft     = state.agents[0].fp_left;
            p1.lastSignal = state.agents[0].last_signal;
            p1.nftId      = state.agents[0].nft_id;
        }

        if (p2 && state.agents != null && state.agents.Count > 1)
        {
            p2.fpLeft     = state.agents[1].fp_left;
            p2.lastSignal = state.agents[1].last_signal;
            p2.nftId      = state.agents[1].nft_id;
        }
    }

    private void SyncMatchVisuals(LilypadState state, int playerIndex)
    {
        AgentRunTime p1 = GameManager.Instance.LeftAgent;
        AgentRunTime p2 = GameManager.Instance.RightAgent;

        // Sync HP
        if (p1 && state.agents.Count > 0)
        {
            p1.maxHealth = Mathf.RoundToInt(state.agents[0].stats.hp_max);
            p1.currentHealth = Mathf.RoundToInt(state.agents[0].stats.hp_current);
        }
        if (p2 && state.agents.Count > 1)
        {
            p2.maxHealth = Mathf.RoundToInt(state.agents[1].stats.hp_max);
            p2.currentHealth = Mathf.RoundToInt(state.agents[1].stats.hp_current);
        }

        UIManager.Instance.UpdateHealthBars(p1, p2);
        
        // Sync Emotions
        string p1Emotion = state.agents.Count > 0 ? state.agents[0].emotional_state : "Unknown";
        string p2Emotion = state.agents.Count > 1 ? state.agents[1].emotional_state : "Unknown";
        UIManager.Instance.UpdateEmotionDisplays(p1Emotion, p2Emotion);

        // Sync FP for the player's side (also locks buttons if fp=0)
        if (state.agents.Count > playerIndex)
        {
            int fp = state.agents[playerIndex].fp_left;
            UIManager.Instance.SyncFPButtons(fp);
        }

        // Sync FP lights for the opponent's side (display only — no button logic)
        int opponentIndex = playerIndex == 0 ? 1 : 0;
        if (state.agents.Count > opponentIndex)
        {
            int opponentFp = state.agents[opponentIndex].fp_left;
            UIManager.Instance.UpdateOpponentFPLights(opponentFp, opponentIndex == 0);
        }

        // ── Charging state ────────────────────────────────────────────────────
        // Per spec: when agents[playerIndex].charging == true, the Special resolve
        // fires automatically next round. We flag it here — the actual null submit
        // happens AFTER ProcessQueueAsync fully exits, so the queue is empty and
        // _isPlaying = false before the backend response arrives.
        if (state.agents.Count > playerIndex && state.agents[playerIndex].charging)
        {
            _pendingChargingSubmit = true;
        }
    }

    // WebGL-safe delay — Task.Delay is unreliable in WebGL builds.
    // This bridges a coroutine WaitForSeconds into an awaitable Task.
    private Task WaitSeconds(float seconds)
    {
        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(WaitCoroutine(seconds, tcs));
        return tcs.Task;
    }

    private IEnumerator WaitCoroutine(float seconds, TaskCompletionSource<bool> tcs)
    {
        yield return new WaitForSeconds(seconds);
        tcs.SetResult(true);
    }

    private Task WaitForCoroutine(IEnumerator coroutine)
    {
        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(WrapCoroutine(coroutine, tcs));
        return tcs.Task;
    }

    private IEnumerator WrapCoroutine(IEnumerator coroutine, TaskCompletionSource<bool> tcs)
    {
        yield return StartCoroutine(coroutine);
        tcs.SetResult(true);
    }
    
    private void EndMatch(AgentRunTime loser)
    {
        if (_matchEnded) return;
        _matchEnded = true;

        loser.visualController.StopLowHpFlicker();
        loser.visualController.PlayDeath();

        AgentRunTime winner =
            loser == GameManager.Instance.LeftAgent
                ? GameManager.Instance.RightAgent
                : GameManager.Instance.LeftAgent;

        string winnerName = winner ? winner.DisplayName : "Unknown";

        if (MenuManager.Instance)
        {
            MenuManager.Instance.ShowGameOverMenu(winnerName);
        }

        UIManager.Instance.SetButtonsInteractable(false);
        UIManager.Instance.ToggleActionPanel(false);
    }

    public void ResetState()
    {
        _eventQueue.Clear();
        _isPlaying = false;
        _matchEnded = false;
        _lastPlayedRound = 0;
        _currentPlayerAssigned = null;
        _introTask = null;
        _pendingChargingSubmit = false;
        Debug.Log("[ActionManager] State reset.");
    }
}