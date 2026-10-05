using UnityEngine;
using Newtonsoft.Json;

public class WebGLReceiver : MonoBehaviour
{
    //Receives the pre-match NFT data (Wallet Addresses, Body Parts)
    public void InitializeMatch(string setupJson)
    {
        Debug.Log($"[WebGL Receiver] Match Initialization Received: {setupJson}");

        // Register intro FIRST so the queue waits before any state is processed
        _ = UIManager.Instance.ShowStartFight();
        
        MatchSetupPayload setupData = JsonConvert.DeserializeObject<MatchSetupPayload>(setupJson);
        if (setupData == null) 
        {
            Debug.LogError("[WebGL Receiver] Failed to parse MatchSetupPayload!");
            return;
        }

        // Apply traits to Player 1 (Left Agent)
        if (GameManager.Instance.LeftAgent && GameManager.Instance.LeftAgent.visualController)
        {
            GameManager.Instance.LeftAgent.visualController.ApplyNFTTraits(setupData.player1.traits);
            UIManager.Instance.UpdatePlayerNames(setupData.player1.wallet_address, true);
            if (int.TryParse(setupData.player1.nft_id, out int p1NftId))
                GameManager.Instance.LeftAgent.nftId = p1NftId;
        }

        // Apply traits to Player 2 (Right Agent)
        if (GameManager.Instance.RightAgent && GameManager.Instance.RightAgent.visualController)
        {
            GameManager.Instance.RightAgent.visualController.ApplyNFTTraits(setupData.player2.traits);
            UIManager.Instance.UpdatePlayerNames(setupData.player2.wallet_address, false);
            if (int.TryParse(setupData.player2.nft_id, out int p2NftId))
                GameManager.Instance.RightAgent.nftId = p2NftId;
        }

        // Show NFT IDs in the turn indicators (#2553 etc.)
        UIManager.Instance.ShowPlayerNames();

        Debug.Log($"P1 Wallet: {setupData.player1.wallet_address}, P1 Hat: {setupData.player1.traits.skin}");
        
        MenuManager.Instance.StartGame();
    }

    // Receives the actual combat loop JSON from the Rust backend
    public void ReceiveStateUpdate(string jsonPayload)
    {
        Debug.Log($"[WebGL Receiver] State Update Received");
        ActionManager.Instance.EnqueueState(jsonPayload);
    }
}