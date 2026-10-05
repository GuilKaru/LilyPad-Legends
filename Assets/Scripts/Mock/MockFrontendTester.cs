using UnityEngine;

public class MockFrontendTester : MonoBehaviour
{
    [Header("Drag your WebGLReceiver here")]
    public WebGLReceiver targetReceiver;

    private void OnEnable()
    {
        // Mirror what React does: when Play is pressed, respond with InitializeMatch
        JSBridge.OnMockMatchRequested += SimulateMatchInitialization;
    }

    private void OnDisable()
    {
        JSBridge.OnMockMatchRequested -= SimulateMatchInitialization;
    }

    void Update()
    {
        // Press 'T' to manually trigger initialization (bypass menu for quick testing)
        if (Input.GetKeyDown(KeyCode.T))
        {
            SimulateMatchInitialization();
        }
    }

    [ContextMenu("Test Match Initialization")]
    public void SimulateMatchInitialization()
    {
        if (!targetReceiver)
        {
            Debug.LogError("[Mock] Please assign the WebGLReceiver in the inspector!");
            return;
        }

        // Using real example IDs to test turn indicator display (#2553 and #101)
        string mockSetupJson = @"
        {
          ""player1"": {
            ""wallet_address"": ""0x76c87cf7813f725a457f713020d59f32eb494953718f978be1d916c5a02d4aa2"",
            ""nft_id"": ""2553"",
            ""traits"": {
              ""hat"": ""minime"",
              ""skin"": ""leaf_frog"",
              ""mouth"": ""one_tooth"",
              ""eyes"": ""sunglasses_brown""
            }
          },
          ""player2"": {
            ""wallet_address"": ""0xAI"",
            ""nft_id"": ""101"",
            ""traits"": {
              ""hat"": ""bareheaded"",
              ""skin"": ""talus_gradient"",
              ""mouth"": ""stunned"",
              ""eyes"": ""vertical_blue""
            }
          }
        }";

        Debug.Log("[Mock Frontend] Sending Initialization JSON to Unity...");
        targetReceiver.InitializeMatch(mockSetupJson);
    }
}