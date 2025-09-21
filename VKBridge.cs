using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class VKBridge : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void SendVKMessage(string message);

    public static void Send(string method, string parameters = "{}")
    {
        // Проверяем, что параметры не пустые
        if (string.IsNullOrWhiteSpace(parameters))
        {
            Debug.LogWarning("Empty parameters detected. Replacing with default JSON.");
            parameters = "{}"; // Устанавливаем пустой объект JSON
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        var message = new VKBridgeMessage { method = method, parameters = parameters };
        var jsonMessage = JsonUtility.ToJson(message);
        SendVKMessage(jsonMessage);
#else
        Debug.Log($"[VK Bridge Mock] Would send: {method} with params {parameters}");
#endif
    }
}

[Serializable]
public class VKBridgeMessage
{
    public string method;
    public string parameters;
}