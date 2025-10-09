using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace OnePoker.Network
{
    public class HttpManager : MonoBehaviour
    {
        private static HttpManager _instance;
        public static HttpManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("HttpManager");
                    _instance = go.AddComponent<HttpManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public const string ApiBaseUrl = "https://5tp37snsbk.execute-api.ap-northeast-1.amazonaws.com/dev";

        [Serializable]
        private class GenericErrorResponse
        {
            // For API Gateway/Lambda errors
            public string error; 
            public string message;
            // For direct Cognito errors
            public string __type;
        }

        public void Post<TResponse>(
            string url,
            string jsonBody,
            Action<TResponse> onSuccess,
            Action<string> onError,
            Dictionary<string, string> headers = null)
        {
            StartCoroutine(PostCoroutine(url, jsonBody, onSuccess, onError, headers));
        }

        public void Get<TResponse>(
            string url,
            Action<TResponse> onSuccess,
            Action<string> onError,
            Dictionary<string, string> headers = null)
        {
            StartCoroutine(GetCoroutine(url, onSuccess, onError, headers));
        }

        public void GetGameState(
            string gameId,
            string playerId,
            Action<string> onSuccess,
            Action<string> onError)
        {
            string url = $"{ApiBaseUrl}/get-state?gameId={gameId}&playerId={playerId}";
            Debug.Log($"HttpManager - Calling get-game-state API: {url}");
            
            Get<GameStateResponse>(
                url,
                (response) => {
                    string responseJson = JsonUtility.ToJson(response);
                    onSuccess?.Invoke(responseJson);
                },
                onError
            );
        }

        public void SetPhaseTransitionTime(
            string gameId,
            string playerId,
            int transitionDelay,
            Action<string> onSuccess,
            Action<string> onError)
        {
            string url = $"{ApiBaseUrl}/set-phase-transition";
            string jsonBody = JsonUtility.ToJson(new SetPhaseTransitionRequest
            {
                gameId = gameId,
                playerId = playerId,
                transitionDelay = transitionDelay
            });
            
            Debug.Log($"HttpManager - Calling set-phase-transition API: {url}, body: {jsonBody}");
            
            Post<SetPhaseTransitionResponse>(
                url,
                jsonBody,
                (response) => {
                    string responseJson = JsonUtility.ToJson(response);
                    onSuccess?.Invoke(responseJson);
                },
                onError
            );
        }

        public void SendBetAction(
            string gameId,
            string playerId,
            string actionType,
            int betValue,
            Action<string> onSuccess,
            Action<string> onError)
        {
            string url = $"{ApiBaseUrl}/bet-action";
            string jsonBody = JsonUtility.ToJson(new BetActionRequest
            {
                gameId = gameId,
                playerId = playerId,
                actionType = actionType,
                betValue = betValue
            });
            
            Debug.Log($"HttpManager - Calling bet-action API: {url}, body: {jsonBody}");
            
            Post<BetActionResponse>(
                url,
                jsonBody,
                (response) => {
                    string responseJson = JsonUtility.ToJson(response);
                    onSuccess?.Invoke(responseJson);
                },
                onError
            );
        }

        public void UpdateGameState(
            string gameId,
            string playerId,
            string actionType,
            object actionData,
            Action<string> onSuccess,
            Action<string> onError)
        {
            string url = $"{ApiBaseUrl}/update-state";
            string jsonBody = JsonUtility.ToJson(new UpdateGameStateRequest
            {
                gameId = gameId,
                playerId = playerId,
                actionType = actionType,
                actionData = actionData
            });
            
            Debug.Log($"HttpManager - Calling update-state API: {url}, body: {jsonBody}");
            
            Post<UpdateGameStateResponse>(
                url,
                jsonBody,
                (response) => {
                    string responseJson = JsonUtility.ToJson(response);
                    onSuccess?.Invoke(responseJson);
                },
                onError
            );
        }

        public void UpdatePlayerBetAmount(
            string gameId,
            string playerId,
            int betAmount,
            Action<string> onSuccess,
            Action<string> onError)
        {
            string url = $"{ApiBaseUrl}/update-state";
            string jsonBody = JsonUtility.ToJson(new UpdatePlayerBetAmountRequest
            {
                gameId = gameId,
                playerId = playerId,
                betAmount = betAmount
            });
            
            Debug.Log($"HttpManager - Calling update-state API for bet amount: {url}, body: {jsonBody}");
            
            Post<UpdatePlayerBetAmountResponse>(
                url,
                jsonBody,
                (response) => {
                    string responseJson = JsonUtility.ToJson(response);
                    onSuccess?.Invoke(responseJson);
                },
                onError
            );
        }

        [System.Serializable]
        private class GameStateResponse
        {
            public string gameId;
            public string gamePhase;
            public long? phaseTransitionTime; // nullableに変更
            public string currentTurn;
            public int player1Life;
            public int player2Life;
            public int currentBet;
            public bool player1CardPlaced;
            public bool player2CardPlaced;
            public int[] myCards;
            public int myLife;
            public int myBetAmount;
            public bool myCardPlaced;
            public bool opponentCardPlaced;
            public int? opponentPlacedCardId;
            public string updatedAt;
            public int player1BetAmount; // 親のBet金額
            public bool player1Set; // プレイヤー1のセット完了状態
            public bool player2Set; // プレイヤー2のセット完了状態
            public int player1CardValue; // プレイヤー1がセットしたカード値
            public int player2CardValue; // プレイヤー2がセットしたカード値
            public string awaitingPlayer; // 待機中のプレイヤー "P1", "P2", "none"
            public int currentRequiredBet; // 現在必要なベット額
            public string[] player1UsedSkills; // プレイヤー1の使用済スキル
            public string[] player2UsedSkills; // プレイヤー2の使用済スキル
        }

        [System.Serializable]
        private class SetPhaseTransitionRequest
        {
            public string gameId;
            public string playerId;
            public int transitionDelay;
        }

        [System.Serializable]
        private class SetPhaseTransitionResponse
        {
            public bool success;
            public long phaseTransitionTime;
            public string message;
        }

        [System.Serializable]
        private class UpdateGameStateRequest
        {
            public string gameId;
            public string playerId;
            public string actionType;
            public object actionData;
        }

        [System.Serializable]
        private class UpdateGameStateResponse
        {
            public bool success;
            public string message;
        }

        [System.Serializable]
        private class UpdatePlayerBetAmountRequest
        {
            public string gameId;
            public string playerId;
            public int betAmount;
        }

        [System.Serializable]
        private class UpdatePlayerBetAmountResponse
        {
            public bool success;
            public string message;
            public int player1BetAmount;
        }

        [System.Serializable]
        private class BetActionRequest
        {
            public string gameId;
            public string playerId;
            public string actionType; // "call", "raise", "drop"
            public int betValue;
        }

        [System.Serializable]
        private class BetActionResponse
        {
            public string gameId;
            public string gamePhase;
            public bool isMyTurn;
            public int currentBet;
            public OpponentAction opponentAction;
            public string message;
        }

        [System.Serializable]
        private class OpponentAction
        {
            public string actionType; // "call", "raise", "drop"
            public int betValue;
            public string playerId;
        }

        private IEnumerator PostCoroutine<TResponse>(
            string url,
            string jsonBody,
            Action<TResponse> onSuccess,
            Action<string> onError,
            Dictionary<string, string> headers)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // Set default and custom headers
            request.SetRequestHeader("Content-Type", "application/json");
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseJson = request.downloadHandler.text;
                // Some successful responses might be empty (e.g., Cognito's ConfirmSignUp)
                if (string.IsNullOrEmpty(responseJson))
                {
                    onSuccess?.Invoke(default); // Return default for the type (e.g., null)
                }
                else
                {
                    try
                    {
                        var responseData = JsonUtility.FromJson<TResponse>(responseJson);
                        onSuccess?.Invoke(responseData);
                    }
                    catch (Exception e)
                    {
                        string parseErrorMessage = $"Failed to parse JSON response: {responseJson}\nError: {e.Message}";
                        Debug.LogError(parseErrorMessage);
                        onError?.Invoke("Failed to parse server response.");
                    }
                }
            }
            else
            {
                string errorJson = request.downloadHandler.text;
                string errorMessage = $"Error {request.responseCode}: {request.error}";

                if (!string.IsNullOrEmpty(errorJson))
                {
                    try
                    {
                        var errorResponse = JsonUtility.FromJson<GenericErrorResponse>(errorJson);
                        if (!string.IsNullOrEmpty(errorResponse.message)) errorMessage = errorResponse.message;
                        else if (!string.IsNullOrEmpty(errorResponse.error)) errorMessage = errorResponse.error;
                        else if (!string.IsNullOrEmpty(errorResponse.__type)) errorMessage = errorResponse.__type.Split('#')[1]; // Cognito specific
                        else errorMessage = errorJson;
                    }
                    catch {
                        errorMessage = errorJson; // Fallback to raw text
                    }
                }
                onError?.Invoke(errorMessage);
            }
        }

        private IEnumerator GetCoroutine<TResponse>(
            string url,
            Action<TResponse> onSuccess,
            Action<string> onError,
            Dictionary<string, string> headers)
        {
            using var request = new UnityWebRequest(url, "GET");
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // Set default and custom headers
            request.SetRequestHeader("Content-Type", "application/json");
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseJson = request.downloadHandler.text;
                // Some successful responses might be empty
                if (string.IsNullOrEmpty(responseJson))
                {
                    onSuccess?.Invoke(default);
                }
                else
                {
                    try
                    {
                        var responseData = JsonUtility.FromJson<TResponse>(responseJson);
                        onSuccess?.Invoke(responseData);
                    }
                    catch (Exception e)
                    {
                        string parseErrorMessage = $"Failed to parse JSON response: {responseJson}\nError: {e.Message}";
                        Debug.LogError(parseErrorMessage);
                        onError?.Invoke("Failed to parse server response.");
                    }
                }
            }
            else
            {
                string errorJson = request.downloadHandler.text;
                string errorMessage = $"Error {request.responseCode}: {request.error}";

                if (!string.IsNullOrEmpty(errorJson))
                {
                    try
                    {
                        var errorResponse = JsonUtility.FromJson<GenericErrorResponse>(errorJson);
                        if (!string.IsNullOrEmpty(errorResponse.message)) errorMessage = errorResponse.message;
                        else if (!string.IsNullOrEmpty(errorResponse.error)) errorMessage = errorResponse.error;
                        else if (!string.IsNullOrEmpty(errorResponse.__type)) errorMessage = errorResponse.__type.Split('#')[1];
                        else errorMessage = errorJson;
                    }
                    catch {
                        errorMessage = errorJson;
                    }
                }
                onError?.Invoke(errorMessage);
            }
        }

        /// <summary>
        /// スキル使用API
        /// </summary>
        public void UseSkill(string gameId, string playerId, string skillType, Action<UseSkillResponse> onSuccess, Action<string> onError)
        {
            string url = $"{ApiBaseUrl}/use-skill";
            string jsonBody = JsonUtility.ToJson(new UseSkillRequest
            {
                gameId = gameId,
                playerId = playerId,
                skillType = skillType
            });

            Debug.Log($"HttpManager - Using skill: {skillType} for player {playerId} in game {gameId}");
            Debug.Log($"HttpManager - Request URL: {url}");
            Debug.Log($"HttpManager - Request Body: {jsonBody}");

            Post<UseSkillResponse>(url, jsonBody, onSuccess, onError);
        }

        [System.Serializable]
        public class UseSkillRequest
        {
            public string gameId;
            public string playerId;
            public string skillType; // "Scan", "Change", "Obstruct", "FakeOut", "Copy"
        }

        [System.Serializable]
        public class UseSkillResponse
        {
            public bool success;
            public string gameId;
            public string playerId;
            public string skillType;
            public string[] usedSkills;
        }
    }
} 