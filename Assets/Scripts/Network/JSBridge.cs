using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Newtonsoft.Json;

public class JSBridge : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void SendActionToBackend(string actionStr);

    [DllImport("__Internal")]
    private static extern void SendMatchRequestToJS();

    public static event Action<string> OnMockActionSent;
    public static event Action OnMockMatchRequested;

    // Called when the player presses Play on the Main Menu.
    // Fires "LilypadRequestMatch" on window so React can start matchmaking.
    public static void SendMatchRequest()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SendMatchRequestToJS();
#else
        Debug.Log("[Mock JS Bridge] Match Requested.");
        OnMockMatchRequested?.Invoke();
#endif
    }

    public static void SendAction(PlayerMovePayload movePayload)
    {
        string jsonAction = JsonConvert.SerializeObject(movePayload);
#if UNITY_WEBGL && !UNITY_EDITOR
        SendActionToBackend(jsonAction);
#else
        Debug.Log($"[Mock JS Bridge] Sent Action: {jsonAction}");
        OnMockActionSent?.Invoke(jsonAction);
#endif
    }
}