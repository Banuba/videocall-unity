using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class TokenObject
{
    public string rtcToken;
}

namespace AgoraUtilities
{
    public static class HelperClass
    {
        public static IEnumerator FetchToken(string url, string channel, int userId, Action<string> callback = null)
        {
            UnityWebRequest request = UnityWebRequest.Get($"{url}/rtc/{channel}/publisher/uid/{userId}/");
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log(request.error);
                callback?.Invoke(null);
                yield break;
            }
            TokenObject tokenInfo = JsonUtility.FromJson<TokenObject>(request.downloadHandler.text);
            callback?.Invoke(tokenInfo.rtcToken);
        }
    }
}