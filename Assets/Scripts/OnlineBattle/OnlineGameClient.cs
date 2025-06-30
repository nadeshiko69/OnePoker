using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

public class OnlineGameClient
{
    private string apiBaseUrl;

    public OnlineGameClient(string apiBaseUrl)
    {
        this.apiBaseUrl = apiBaseUrl;
    }

    /// <summary>
    /// ゲーム状態を取得
    /// </summary>
    public void GetGameState(string gameId, string playerId, Action<GameStateResponse> onSuccess, Action<string> onError)
    {
        string url = $"{apiBaseUrl}/game/state?gameId={gameId}&playerId={playerId}";
        MonoBehaviour.StartCoroutine(GetRequest(url, onSuccess, onError));
    }

    /// <summary>
    /// ゲーム状態を更新
    /// </summary>
    public void UpdateGameState(string gameId, string playerId, string actionType, Dictionary<string, object> actionData, Action<ActionResponse> onSuccess, Action<string> onError)
    {
        string url = $"{apiBaseUrl}/game/update";
        
        var requestData = new UpdateGameStateRequest
        {
            gameId = gameId,
            playerId = playerId,
            actionType = actionType,
            actionData = actionData
        };

        string jsonData = JsonUtility.ToJson(requestData);
        MonoBehaviour.StartCoroutine(PostRequest(url, jsonData, onSuccess, onError));
    }

    /// <summary>
    /// GETリクエストを送信
    /// </summary>
    private IEnumerator GetRequest<T>(string url, Action<T> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    T response = JsonUtility.FromJson<T>(request.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"Failed to parse response: {e.Message}");
                }
            }
            else
            {
                onError?.Invoke($"Request failed: {request.error}");
            }
        }
    }

    /// <summary>
    /// POSTリクエストを送信
    /// </summary>
    private IEnumerator PostRequest<T>(string url, string jsonData, Action<T> onSuccess, Action<string> onError)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    T response = JsonUtility.FromJson<T>(request.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"Failed to parse response: {e.Message}");
                }
            }
            else
            {
                onError?.Invoke($"Request failed: {request.error}");
            }
        }
    }
}

// リクエスト・レスポンスクラス

[Serializable]
public class UpdateGameStateRequest
{
    public string gameId;
    public string playerId;
    public string actionType;
    public Dictionary<string, object> actionData;
}

[Serializable]
public class GameStateResponse
{
    public string gameId;
    public string roomCode;
    public string player1Id;
    public string player2Id;
    public string currentTurn;
    public string gamePhase;
    public List<int> myCards;
    public int myLife;
    public int myBetAmount;
    public bool myCardPlaced;
    public int player1Life;
    public int player2Life;
    public int currentBet;
    public bool player1CardPlaced;
    public bool player2CardPlaced;
    public bool opponentCardPlaced;
    public int? opponentPlacedCardId;
    public long updatedAt;
}

[Serializable]
public class ActionResponse
{
    public bool success;
    public string message;
    public Dictionary<string, object> updatedGameState;
}

// MonoBehaviour拡張メソッド
public static class MonoBehaviourExtensions
{
    public static Coroutine StartCoroutine(this MonoBehaviour mb, IEnumerator routine)
    {
        return mb.StartCoroutine(routine);
    }
} 