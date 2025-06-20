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
    }
} 