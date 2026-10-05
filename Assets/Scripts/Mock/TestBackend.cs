using UnityEngine;
using System.Collections;
using Newtonsoft.Json;

public class TestBackend : MonoBehaviour
{
    private int _currentRound = 0;

    private float _p1Hp    = 300f;
    private float _p1HpMax = 300f;
    private float _p2Hp    = 300f;
    private float _p2HpMax = 300f;

    private int _p1Fp = 8;
    private int _p2Fp = 8;
    private bool _p1Charging = false; // true after player picks SpecialAttack; resolve fires next round
    private bool _p2Charging = false; // true after AI picks SpecialAttack; auto-resolves next round

    private void OnEnable()
    {
        JSBridge.OnMockMatchRequested += OnMatchRequested;
        JSBridge.OnMockActionSent     += HandlePlayerMove;
    }

    private void OnDisable()
    {
        JSBridge.OnMockMatchRequested -= OnMatchRequested;
        JSBridge.OnMockActionSent     -= HandlePlayerMove;
    }

    // ── Called when Play button is pressed (via JSBridge.OnMockMatchRequested) ──
    private void OnMatchRequested()
    {
        ResetState();
        StartCoroutine(StartMatch());
    }

    void Update()
    {
        // SPACE: manually re-send initial state for debugging
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetState();
            StartCoroutine(StartMatch());
        }
    }

    private void ResetState()
    {
        _currentRound = 0;
        _p1Hp = _p1HpMax = 300f;
        _p2Hp = _p2HpMax = 300f;
        _p1Fp = 8;
        _p2Fp = 8;
        _p1Charging = false;
        _p2Charging = false;
    }

    private IEnumerator StartMatch()
    {
        // Small delay so MockFrontendTester's InitializeMatch finishes first (spawns agents, shows FIGHT)
        yield return new WaitForSeconds(0.15f);

        // Round 0: initial state with no history
        string jsonInit = BuildActionPhase(
            round: 0,
            p1Hp: _p1Hp, p1HpMax: _p1HpMax, p1Fp: _p1Fp, p1Emotion: "Neutral", p1Signal: null, p1Charging: false,
            p2Hp: _p2Hp, p2HpMax: _p2HpMax, p2Fp: _p2Fp, p2Emotion: "Angry",   p2Signal: null,
            roundHistoryJson: "",
            you: 0, currentPlayer: 0
        );
        ActionManager.Instance.EnqueueState(jsonInit);

        // Opening banter (Turn 0) — goes in as Banter phase
        string jsonBanter = BuildBanterPhase(
            round: 1,
            banter0: "Let's get this started!",
            banter1: "You're going down, frog.",
            you: 0, currentPlayer: 0
        );
        ActionManager.Instance.EnqueueState(jsonBanter);

        Debug.Log("[TestBackend] Match started. Waiting for player input...");
    }

    // ── Receives the player's submitted move ───────────────────────────────────
    private void HandlePlayerMove(string jsonPayload)
    {
        Debug.Log($"[TestBackend] Player move received: {jsonPayload}");
        PlayerMovePayload move = JsonConvert.DeserializeObject<PlayerMovePayload>(jsonPayload);
        StartCoroutine(ResolveRound(move));
    }

    private IEnumerator ResolveRound(PlayerMovePayload move)
    {
        yield return new WaitForSeconds(0.8f);

        _currentRound++;

        string[] banterPool0 = { "Come on, is that all you got?", "Nice try!", "My turn now." };
        string[] banterPool1 = { "Pathetic.", "You will regret that.", "Too slow!" };
        string b0 = $"\"{EscapeJson(banterPool0[_currentRound % banterPool0.Length])}\"";
        string b1 = $"\"{EscapeJson(banterPool1[_currentRound % banterPool1.Length])}\"";

        if (move.action == "SpecialAttack")
        {
            // ── CHARGE ROUND: no p1 damage yet, p1 becomes vulnerable ──────────
            if (_p1Fp > 0) _p1Fp--;
            if (_p2Fp > 0) _p2Fp--;
            _p1Charging = true;

            string p1SignalJson = string.IsNullOrEmpty(move.preparing_signal) ? "null" : $"\"{move.preparing_signal}\"";

            // Player may have called before submitting SpecialAttack
            bool   p1Called           = !string.IsNullOrEmpty(move.call_guess);
            string p1CallGuessJson    = p1Called ? $"\"{move.call_guess}\"" : "null";
            // Agent always defends in scripted rounds — GuessDefend succeeds, GuessSpecial fails
            bool   p1CallSuccess      = p1Called && move.call_guess == "GuessDefend";
            string p1CallSuccessJson  = p1Called ? (p1CallSuccess ? "true" : "false") : "null";

            // Agent acts normally this turn — player charging doesn't stop the agent
            bool   agentLiedCharge    = (_currentRound % 2 == 1);
            string p2SignalCharge     = agentLiedCharge ? "\"SigSpecial\"" : "\"SigDefend\"";
            string p2SignalNextCharge = agentLiedCharge ? "SigSpecial" : "SigDefend";
            float  p2DamageCharge    = 18f;
            _p1Hp = Mathf.Max(0f, _p1Hp - p2DamageCharge);

            // Agent calls GuessSpecial this turn — succeeds if player lied (SigDefend but doing SpecialAttack)
            string p2CallGuessCharge     = "\"GuessSpecial\"";
            bool   p2CallSuccessCharge   = !string.IsNullOrEmpty(move.preparing_signal) && move.preparing_signal == "SigDefend";
            string p2CallSuccessChargeStr = p2CallSuccessCharge ? "true" : "false";

            string rh = $@"{{
                ""round"": {_currentRound},
                ""actions"": [""SpecialAttack"", ""Prepare""],
                ""banter"": [{b0}, {b1}],
                ""attack_results"": [null, null],
                ""defend_results"": [null, null],
                ""damage_dealt"": [0, {p2DamageCharge}],
                ""hp_after"": [{_p1Hp}, {_p2Hp}],
                ""block_pcts"": [null, null],
                ""first_actor"": 1,
                ""focus_score"": [3, 4],
                ""charge_vulnerable"": [true, false],
                ""reward_applied"": [null, null],
                ""bluff_caught"": [null, null],
                ""call_success"": [{p1CallSuccessJson}, {p2CallSuccessChargeStr}],
                ""moves"": [
                    {{ ""action"": ""SpecialAttack"", ""emotional_state"": ""Neutral"", ""banter"": null, ""preparing_signal"": {p1SignalJson}, ""call_guess"": {p1CallGuessJson} }},
                    {{ ""action"": ""Prepare"", ""emotional_state"": ""Angry"", ""banter"": null, ""preparing_signal"": {p2SignalCharge}, ""call_guess"": {p2CallGuessCharge} }}
                ],
                ""rng_seed"": 12345
            }}";

            // Send action phase with p1.charging = true — Unity sees it and auto-submits null
            ActionManager.Instance.EnqueueState(BuildActionPhase(
                round: _currentRound,
                p1Hp: _p1Hp, p1HpMax: _p1HpMax, p1Fp: _p1Fp,
                p1Emotion: move.emotional_state ?? "Neutral", p1Signal: null, p1Charging: true,
                p2Hp: _p2Hp, p2HpMax: _p2HpMax, p2Fp: _p2Fp,
                p2Emotion: "Angry", p2Signal: p2SignalNextCharge,
                roundHistoryJson: rh, you: 0, currentPlayer: 0));

            Debug.Log($"[TestBackend] Round {_currentRound}: CHARGE declared. Waiting for auto-null...");
        }
        else if (move.action == null && _p1Charging)
        {
            // ── RESOLVE ROUND: Special fires automatically ────────────────────
            _p1Charging = false;
            if (_p2Fp > 0) _p2Fp--;
            float p1Damage = 40f;
            _p2Hp = Mathf.Max(0f, _p2Hp - p1Damage);

            // Player may have called via AutoSubmitChargingMove
            bool   p1CalledResolve        = !string.IsNullOrEmpty(move.call_guess);
            string p1CallGuessResolveJson = p1CalledResolve ? $"\"{move.call_guess}\"" : "null";
            bool   p1CallSuccessResolve   = p1CalledResolve && move.call_guess == "GuessDefend";
            string p1CallSuccessResolveJson = p1CalledResolve ? (p1CallSuccessResolve ? "true" : "false") : "null";

            // Agent acts normally on the resolve turn too
            bool   agentLiedResolve    = (_currentRound % 2 == 1);
            string p2SignalResolve     = agentLiedResolve ? "\"SigSpecial\"" : "\"SigDefend\"";
            string p2SignalNextResolve = agentLiedResolve ? "SigSpecial" : "SigDefend";
            float  p2DamageResolve    = 18f;
            _p1Hp = Mathf.Max(0f, _p1Hp - p2DamageResolve);

            // Agent calls GuessSpecial on resolve turn — player has no signal so agent call always fails
            string p2CallGuessResolve    = "\"GuessSpecial\"";
            string p2CallSuccessResolve  = "false";

            string rh = $@"{{
                ""round"": {_currentRound},
                ""actions"": [""Resolve"", ""Prepare""],
                ""banter"": [{b0}, {b1}],
                ""attack_results"": [""Hit"", null],
                ""defend_results"": [null, null],
                ""damage_dealt"": [{p1Damage}, {p2DamageResolve}],
                ""hp_after"": [{_p1Hp}, {_p2Hp}],
                ""block_pcts"": [null, null],
                ""first_actor"": 0,
                ""focus_score"": [3, 4],
                ""charge_vulnerable"": [false, false],
                ""reward_applied"": [null, null],
                ""bluff_caught"": [null, null],
                ""call_success"": [{p1CallSuccessResolveJson}, {p2CallSuccessResolve}],
                ""moves"": [
                    {{ ""action"": ""Resolve"", ""emotional_state"": ""Triumphant"", ""banter"": null, ""preparing_signal"": null, ""call_guess"": {p1CallGuessResolveJson} }},
                    {{ ""action"": ""Prepare"", ""emotional_state"": ""Angry"", ""banter"": null, ""preparing_signal"": {p2SignalResolve}, ""call_guess"": {p2CallGuessResolve} }}
                ],
                ""rng_seed"": 12345
            }}";

            ActionManager.Instance.EnqueueState(BuildActionPhase(
                round: _currentRound,
                p1Hp: _p1Hp, p1HpMax: _p1HpMax, p1Fp: _p1Fp,
                p1Emotion: "Triumphant", p1Signal: null, p1Charging: false,
                p2Hp: _p2Hp, p2HpMax: _p2HpMax, p2Fp: _p2Fp,
                p2Emotion: "Angry", p2Signal: p2SignalNextResolve,
                roundHistoryJson: rh, you: 0, currentPlayer: 0));

            Debug.Log($"[TestBackend] Round {_currentRound}: SPECIAL RESOLVED. p1 dealt {p1Damage}.");

            if (_p1Hp > 0 && _p2Hp > 0)
                ActionManager.Instance.EnqueueState(BuildBanterPhase(_currentRound + 1, "", "", 0, 0));
            else
                Debug.Log($"[TestBackend] Match over! P1:{_p1Hp} P2:{_p2Hp}");
        }
        else if (_p2Charging)
        {
            // ── P2 RESOLVE ROUND ─────────────────────────────────────────────
            _p2Charging = false;
            float p1Damage = move.action == "Defend" ? 0f : 25f;
            float p2Damage = 40f;
            _p1Hp = Mathf.Max(0f, _p1Hp - p2Damage);
            _p2Hp = Mathf.Max(0f, _p2Hp - p1Damage);
            if (move.action == "Defend" && _p1Fp > 0) _p1Fp--;

            string p1ActionR = move.action ?? "null";
            string p1ResultR = move.action == "Defend" ? "Miss" : "Hit";
            string rhR = $@"{{
                ""round"": {_currentRound},
                ""actions"": [""{p1ActionR}"", ""Resolve""],
                ""banter"": [{b0}, {b1}],
                ""attack_results"": [""{p1ResultR}"", ""Hit""],
                ""defend_results"": [null, null],
                ""damage_dealt"": [{p1Damage}, {p2Damage}],
                ""hp_after"": [{_p1Hp}, {_p2Hp}],
                ""block_pcts"": [null, null],
                ""first_actor"": 0,
                ""focus_score"": [3, 4],
                ""charge_vulnerable"": [false, false],
                ""reward_applied"": [null, null],
                ""bluff_caught"": [false, false],
                ""call_success"": [null, null],
                ""rng_seed"": 12345
            }}";

            ActionManager.Instance.EnqueueState(BuildActionPhase(
                round: _currentRound,
                p1Hp: _p1Hp, p1HpMax: _p1HpMax, p1Fp: _p1Fp,
                p1Emotion: move.emotional_state ?? "Neutral", p1Signal: null, p1Charging: false,
                p2Hp: _p2Hp, p2HpMax: _p2HpMax, p2Fp: _p2Fp,
                p2Emotion: "Angry", p2Signal: null,
                roundHistoryJson: rhR, you: 0, currentPlayer: 0));

            Debug.Log($"[TestBackend] Round {_currentRound}: P2 SPECIAL RESOLVED. Dealt {p2Damage}.");

            if (_p1Hp > 0 && _p2Hp > 0)
                ActionManager.Instance.EnqueueState(BuildBanterPhase(_currentRound + 1, "", "", 0, 0));
            else
                Debug.Log($"[TestBackend] Match over! P1:{_p1Hp} P2:{_p2Hp}");
        }
        else if (_currentRound <= 4)
        {
            // ── SCRIPTED ROUNDS 1-4: Agent always Defends ────────────────────
            // Round 1: Agent signals SigSpecial (LIE)   → actually Defends. bluff_caught[1]=false, call_success[1]=null
            // Round 2: Agent signals SigDefend  (TRUTH)  → actually Defends. bluff_caught[1]=true,  call_success[1]=true
            // Round 3: Agent signals SigSpecial (LIE)   → actually Defends. bluff_caught[1]=false, call_success[1]=false
            // Round 4: Agent signals SigDefend  (TRUTH)  → actually Defends. bluff_caught[1]=true,  call_success[1]=true

            // Agent always Defends — deals 0 damage to player
            if (_p2Fp > 0) _p2Fp--;

            // Player action resolution — reactive to what player submits
            float p1Damage = move.action == "Defend" ? 0f : 25f;
            _p2Hp = Mathf.Max(0f, _p2Hp - p1Damage);
            if (move.action == "Defend" && _p1Fp > 0) _p1Fp--;

            string p1ActionN    = move.action ?? "FastAttack";
            string p1ResultN    = move.action == "Defend" ? "null" : "\"Hit\"";

            // Player signal and call — reactive to what player submitted
            string p1SignalRaw     = move.preparing_signal ?? "";
            string p1SignalJson    = string.IsNullOrEmpty(p1SignalRaw) ? "null" : ('"' + p1SignalRaw + '"');
            bool   p1Called        = !string.IsNullOrEmpty(move.call_guess);
            string p1CallGuessJson = p1Called ? ('"' + move.call_guess + '"') : "null";

            // Agent actually Defends every round.
            // Player call succeeds if they guessed GuessDefend (agent said SigSpecial on odd rounds,
            // but actually Defended — so GuessDefend is WRONG on odd, RIGHT on even... 
            // Actually: call_guess is about what you think they DID, not what they said.
            // GuessDefend = "I think you actually Defended" → correct since agent always Defends.
            // GuessSpecial = "I think you actually Special'd" → wrong since agent always Defends.
            bool   p1CallSuccess     = p1Called && move.call_guess == "GuessDefend";
            string p1CallSuccessJson = p1Called ? (p1CallSuccess ? "true" : "false") : "null";

            // Scripted Agent signal shown IN this round's moves (what they claimed)
            // Odd rounds (1,3): SigSpecial lie. Even rounds (2,4): SigDefend truth.
            bool   agentLiedThisRound  = (_currentRound % 2 == 1);
            string p2SignalInRound      = agentLiedThisRound ? "\"SigSpecial\"" : "\"SigDefend\"";

            // Signal to show player NEXT round so they can call it
            // Odd rounds show SigSpecial (lie), even show SigDefend (truth)
            string p2SignalForNextState = agentLiedThisRound ? "SigSpecial" : "SigDefend";

            // Agent always calls GuessSpecial (reacts to player's signal)
            // Succeeds if player's signal doesn't match their action (player lied)
            bool   p2AgentCalled          = !string.IsNullOrEmpty(move.preparing_signal);
            string p2AgentCallGuess       = p2AgentCalled ? "\"GuessSpecial\"" : "null";
            bool   p2AgentCallSuccessBool = p2AgentCalled && move.preparing_signal != "SigSpecial";
            string p2AgentCallSuccessStr  = p2AgentCalled ? (p2AgentCallSuccessBool ? "true" : "false") : "null";
            // bluff_caught[1]: did player catch the agent's bluff?
            //   true  = agent's bluff was caught (player successfully called it)
            //   false = agent's bluff was NOT caught
            // call_success[1]: did agent's call succeed?
            //   null = no call was made (round 1), true/false otherwise
            string p2BluffCaught;
            string p2CallSuccess;
            switch (_currentRound)
            {
                case 1:  p2BluffCaught = "false"; p2CallSuccess = "null";  break; // lie, no prior call
                case 2:  p2BluffCaught = "true";  p2CallSuccess = "true";  break; // truth, agent call succeeds
                case 3:  p2BluffCaught = "false"; p2CallSuccess = "false"; break; // lie, agent call fails
                case 4:  p2BluffCaught = "true";  p2CallSuccess = "true";  break; // truth, agent call succeeds
                default: p2BluffCaught = "false"; p2CallSuccess = "null";  break;
            }

            // bluff_caught[0]: did agent catch PLAYER's bluff?
            string p1BluffCaught = p1Called ? (p1CallSuccess ? "false" : "true") : "false";

            // Agent always has a signal in scripted rounds → "Prepare" (fog of war)
            string p2ActionScripted = "Prepare";

            string rhS = $@"{{
                ""round"": {_currentRound},
                ""actions"": [""{p1ActionN}"", ""{p2ActionScripted}""],
                ""banter"": [{b0}, {b1}],
                ""attack_results"": [{p1ResultN}, null],
                ""defend_results"": [null, null],
                ""damage_dealt"": [{p1Damage}, 0],
                ""hp_after"": [{_p1Hp}, {_p2Hp}],
                ""block_pcts"": [null, null],
                ""first_actor"": 1,
                ""focus_score"": [3, 4],
                ""charge_vulnerable"": [false, false],
                ""reward_applied"": [null, null],
                ""bluff_caught"": [{p1BluffCaught}, {p2BluffCaught}],
                ""call_success"": [{p1CallSuccessJson}, {p2AgentCallSuccessStr}],
                ""moves"": [
                    {{ ""action"": ""{p1ActionN}"", ""emotional_state"": ""Neutral"", ""banter"": null, ""preparing_signal"": {p1SignalJson}, ""call_guess"": {p1CallGuessJson} }},
                    {{ ""action"": ""{p2ActionScripted}"", ""emotional_state"": ""Angry"", ""banter"": null, ""preparing_signal"": {p2SignalInRound}, ""call_guess"": {p2AgentCallGuess} }}
                ],
                ""rng_seed"": 12345
            }}";

            ActionManager.Instance.EnqueueState(BuildActionPhase(
                round: _currentRound,
                p1Hp: _p1Hp, p1HpMax: _p1HpMax, p1Fp: _p1Fp,
                p1Emotion: move.emotional_state ?? "Neutral", p1Signal: null, p1Charging: false,
                p2Hp: _p2Hp, p2HpMax: _p2HpMax, p2Fp: _p2Fp,
                p2Emotion: "Angry", p2Signal: p2SignalForNextState,
                roundHistoryJson: rhS, you: 0, currentPlayer: 0));

            if (_p1Hp > 0 && _p2Hp > 0)
            {
                ActionManager.Instance.EnqueueState(BuildBanterPhase(_currentRound + 1, "", "", 0, 0));
                Debug.Log($"[TestBackend] Round {_currentRound}: Agent Defend (scripted). Waiting for next move...");
            }
            else
                Debug.Log($"[TestBackend] Match over! P1:{_p1Hp} P2:{_p2Hp}");
        }
        else
        {
            // ── NORMAL ROUND (round 5+) ──────────────────────────────────────
            bool p2Defends = false;
            if (_p2Fp > 0) _p2Fp--;

            float p1Damage = move.action == "Defend" ? 0f : 25f;
            float p2Damage = 18f;
            _p1Hp = Mathf.Max(0f, _p1Hp - p2Damage);
            _p2Hp = Mathf.Max(0f, _p2Hp - p1Damage);
            if (move.action == "Defend" && _p1Fp > 0) _p1Fp--;

            string p1ActionN    = move.action ?? "FastAttack";
            string p1ResultN    = move.action == "Defend" ? "null" : "\"Hit\"";

            string p1SignalRaw     = move.preparing_signal ?? "";
            string p1SignalJson    = string.IsNullOrEmpty(p1SignalRaw) ? "null" : ('"' + p1SignalRaw + '"');
            bool   p1Called        = !string.IsNullOrEmpty(move.call_guess);
            string p1CallGuessJson = p1Called ? ('"' + move.call_guess + '"') : "null";

            bool   p1CallSuccess     = p1Called && move.call_guess == "GuessSpecial" && !p2Defends;
            string p1CallSuccessJson = p1Called ? (p1CallSuccess ? "true" : "false") : "null";
            string p1BluffCaught     = p1Called ? (p1CallSuccess ? "false" : "true") : "false";

            bool   p2Called        = !string.IsNullOrEmpty(move.preparing_signal);
            string p2CallGuessN    = p2Called ? "\"GuessSpecial\"" : "null";
            bool   p2CallSuccessN  = p2Called && move.preparing_signal != "SigSpecial";
            string p2CallSuccessNStr = p2Called ? (p2CallSuccessN ? "true" : "false") : "null";

            string rhN = $@"{{
                ""round"": {_currentRound},
                ""actions"": [""{p1ActionN}"", ""FastAttack""],
                ""banter"": [{b0}, {b1}],
                ""attack_results"": [{p1ResultN}, null],
                ""defend_results"": [null, null],
                ""damage_dealt"": [{p1Damage}, {p2Damage}],
                ""hp_after"": [{_p1Hp}, {_p2Hp}],
                ""block_pcts"": [null, null],
                ""first_actor"": 1,
                ""focus_score"": [3, 4],
                ""charge_vulnerable"": [false, false],
                ""reward_applied"": [null, null],
                ""bluff_caught"": [{p1BluffCaught}, false],
                ""call_success"": [{p1CallSuccessJson}, {p2CallSuccessNStr}],
                ""moves"": [
                    {{ ""action"": ""{p1ActionN}"", ""emotional_state"": ""Neutral"", ""banter"": null, ""preparing_signal"": {p1SignalJson}, ""call_guess"": {p1CallGuessJson} }},
                    {{ ""action"": ""FastAttack"", ""emotional_state"": ""Angry"", ""banter"": null, ""preparing_signal"": null, ""call_guess"": {p2CallGuessN} }}
                ],
                ""rng_seed"": 12345
            }}";

            ActionManager.Instance.EnqueueState(BuildActionPhase(
                round: _currentRound,
                p1Hp: _p1Hp, p1HpMax: _p1HpMax, p1Fp: _p1Fp,
                p1Emotion: move.emotional_state ?? "Neutral", p1Signal: null, p1Charging: false,
                p2Hp: _p2Hp, p2HpMax: _p2HpMax, p2Fp: _p2Fp,
                p2Emotion: "Angry", p2Signal: null,
                roundHistoryJson: rhN, you: 0, currentPlayer: 0));

            if (_p1Hp > 0 && _p2Hp > 0)
            {
                ActionManager.Instance.EnqueueState(BuildBanterPhase(_currentRound + 1, "", "", 0, 0));
                Debug.Log($"[TestBackend] Round {_currentRound}: Normal round. Waiting for next move...");
            }
            else
                Debug.Log($"[TestBackend] Match over! P1:{_p1Hp} P2:{_p2Hp}");
        }
    }
    // ── JSON builders ────────────────────────────────────────────────────────────

    private string BuildBanterPhase(int round, string banter0, string banter1, int? you, int? currentPlayer)
    {
        string youStr           = you.HasValue ? you.Value.ToString() : "null";
        string currentPlayerStr = currentPlayer.HasValue ? currentPlayer.Value.ToString() : "null";
        string b0               = string.IsNullOrEmpty(banter0) ? "null" : $"\"{EscapeJson(banter0)}\"";
        string b1               = string.IsNullOrEmpty(banter1) ? "null" : $"\"{EscapeJson(banter1)}\"";

        return $@"{{
            ""type"": ""state"",
            ""payload"": {{
                ""you"": {youStr},
                ""current_player"": {currentPlayerStr},
                ""state"": {{
                    ""round"": {round},
                    ""phase"": ""Banter"",
                    ""turn"": 0,
                    ""pending_banter"": [{b0}, {b1}],
                    ""agents"": [
                        {{
                            ""stats"": {{ ""hp_current"": {_p1Hp}, ""hp_max"": {_p1HpMax}, ""attack"": 95.0, ""defense"": 92.0, ""speed"": 88.0 }},
                            ""fp_left"": {_p1Fp},
                            ""charging"": false,
                            ""miss_count"": 0,
                            ""crit_hit_count"": 0,
                            ""nft_id"": 120,
                            ""emotional_state"": ""Neutral"",
                            ""last_signal"": null,
                            ""redacted"": false
                        }},
                        {{
                            ""stats"": {{ ""hp_current"": {_p2Hp}, ""hp_max"": {_p2HpMax}, ""attack"": 93.0, ""defense"": 90.0, ""speed"": 91.0 }},
                            ""fp_left"": {_p2Fp},
                            ""charging"": false,
                            ""miss_count"": 0,
                            ""crit_hit_count"": 0,
                            ""nft_id"": 121,
                            ""emotional_state"": ""Angry"",
                            ""last_signal"": null,
                            ""redacted"": true
                        }}
                    ],
                    ""round_history"": []
                }}
            }}
        }}";
    }

    private string BuildActionPhase(
        int round,
        float p1Hp, float p1HpMax, int p1Fp, string p1Emotion, string p1Signal, bool p1Charging,
        float p2Hp, float p2HpMax, int p2Fp, string p2Emotion, string p2Signal,
        string roundHistoryJson,
        int? you, int? currentPlayer)
    {
        string youStr           = you.HasValue ? you.Value.ToString() : "null";
        string currentPlayerStr = currentPlayer.HasValue ? currentPlayer.Value.ToString() : "null";
        string p1SignalJson     = string.IsNullOrEmpty(p1Signal) ? "null" : $"\"{p1Signal}\"";
        string p2SignalJson     = string.IsNullOrEmpty(p2Signal) ? "null" : $"\"{p2Signal}\"";
        string historyArray     = string.IsNullOrEmpty(roundHistoryJson) ? "" : roundHistoryJson;
        string p1ChargingJson   = p1Charging ? "true" : "false";

        return $@"{{
            ""type"": ""state"",
            ""payload"": {{
                ""you"": {youStr},
                ""current_player"": {currentPlayerStr},
                ""state"": {{
                    ""round"": {round},
                    ""phase"": ""Action"",
                    ""turn"": 0,
                    ""pending_banter"": [null, null],
                    ""agents"": [
                        {{
                            ""stats"": {{ ""hp_current"": {p1Hp}, ""hp_max"": {p1HpMax}, ""attack"": 95.0, ""defense"": 92.0, ""speed"": 88.0 }},
                            ""fp_left"": {p1Fp},
                            ""charging"": {p1ChargingJson},
                            ""miss_count"": 0,
                            ""crit_hit_count"": 0,
                            ""nft_id"": 120,
                            ""emotional_state"": ""{p1Emotion}"",
                            ""last_signal"": {p1SignalJson},
                            ""redacted"": false
                        }},
                        {{
                            ""stats"": {{ ""hp_current"": {p2Hp}, ""hp_max"": {p2HpMax}, ""attack"": 93.0, ""defense"": 90.0, ""speed"": 91.0 }},
                            ""fp_left"": {p2Fp},
                            ""charging"": false,
                            ""miss_count"": 0,
                            ""crit_hit_count"": 0,
                            ""nft_id"": 121,
                            ""emotional_state"": ""{p2Emotion}"",
                            ""last_signal"": {p2SignalJson},
                            ""redacted"": true
                        }}
                    ],
                    ""round_history"": [{historyArray}]
                }}
            }}
        }}";
    }

    private string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}