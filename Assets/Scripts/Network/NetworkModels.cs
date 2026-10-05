using System;
using System.Collections.Generic;

[Serializable]
public class ServerMessage
{
    public string type; // "state", "waiting", "error", etc.
    public StatePayload payload;
}

[Serializable]
public class StatePayload
{
    public LilypadState state;
    public int? you; // 0 = Player 1, 1 = Player 2, null = spectator
    public int? current_player;
}

[Serializable]
public class LilypadState
{
    public int round;
    public string phase;
    public int turn;
    public List<AgentState> agents;
    public List<string> pending_banter;
    public LilypadMove pending_move;
    public List<RoundResult> round_history;
}

[Serializable]
public class AgentState
{
    // Core combat fields
    public BackendAgentStats stats;
    public int fp_left;
    public bool charging;
    public int miss_count;
    public int crit_hit_count;
    public int nft_id;

    // Emotional state
    public string emotional_state;  // One of the 12 EmotionalState enum values

    // Bluff / call
    public string last_signal;      // "SigDefend" | "SigSpecial" | null
    public string last_actual_action; // hidden from opponent; null when redacted

    // Fog-of-war fields (only set when redacted = true)
    public bool redacted;
    public string hp_tier; // "High" | "Medium" | "Low" | "Critical"
    public List<int> stat_comparison; // [atk, def, spd] each -1/0/1 vs viewer

    // Banter context (populated during action phase)
    public string opponent_banter;
}

[Serializable]
public class BackendAgentStats
{
    public float hp_current;
    public float hp_max;
    public float attack;
    public float defense;
    public float speed;
}

[Serializable]
public class LilypadMove
{
    public string action;
    public string emotional_state;
    public string banter;
    public string preparing_signal;
    public string call_guess;
}
[Serializable]
public class RoundResult
{
    public int round;
    public List<string> actions;         // "FastAttack" | "SpecialAttack" | "Defend" | null
    public List<string> banter;          // banter strings per agent
    public List<LilypadMove> moves;      // full move submitted by each agent
    public List<string> attack_results;  // "Miss" | "Hit" | "Crit" | "CritBlocked" | null
    public List<string> defend_results;  // "Fail" | "Normal" | "CritBlock" | null
    public List<float> damage_dealt;
    public List<float> hp_after;
    public List<float?> block_pcts;       // effective block % per agent (null if no defend)
    public int? first_actor;             // 0 or 1
    public List<int> focus_score;        // Focus Score per agent
    public List<bool> charge_vulnerable; // whether each agent was charge-vulnerable this round
    public List<string> reward_applied;  // null | true | false per agent
    public List<bool?> bluff_caught;
    public List<bool?> call_success;      // null if no call was submitted
    public ulong rng_seed;
}

[Serializable]
public class MatchSetupPayload
{
    public PlayerSetupData player1;
    public PlayerSetupData player2;
}

[Serializable]
public class PlayerSetupData
{
    public string wallet_address;
    public string nft_id;
    public NFTTraits traits;
}

[Serializable]
public class NFTTraits
{
    public string hat;
    public string skin;
    public string mouth;
    public string eyes;
}

[Serializable]
public class PlayerMovePayload
{
    public string action;           // "FastAttack", "SpecialAttack", "Defend"
    public string emotional_state; // "Angry", "Triumphant", "Optimistic", etc.
    public string banter;
    public string preparing_signal;
    public string call_guess;
}