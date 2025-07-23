using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class JoinRoomManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputRoomCode;
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI messageText;
    
    [Header("マッチング状態")]
    [SerializeField] private GameObject inputPanel;
    [SerializeField] private GameObject matchedPanel;
    [SerializeField] private TextMeshProUGUI matchedText;

    private string playerId;
    private string roomCode;
    private string opponentId;
    private bool isMatched = false;
    
    // API GatewayのエンドポイントURL
    private string joinUrl = "https://lbr9cfjhuk.execute-api.ap-northeast-1.amazonaws.com/dev/join";
    private string startGameUrl = "https://5tp37snsbk.execute-api.ap-northeast-1.amazonaws.com/dev/start-game";

    private void Start()
    {
        Debug.Log($"[JoinRoom] Start() - 部屋参加画面初期化");
        submitButton.onClick.AddListener(OnSubmit);
        
        // PlayerPrefsからUserData(JSON)を取得し、usernameをパース
        string userDataJson = PlayerPrefs.GetString("UserData", "");
        playerId = "guest";
        if (!string.IsNullOrEmpty(userDataJson))
        {
            var userData = JsonUtility.FromJson<UserData>(userDataJson);
            playerId = userData.username;
        }
        
        Debug.Log($"[JoinRoom] プレイヤーID: {playerId}");
    }

    private void OnSubmit()
    {
        string code = inputRoomCode.text;
        Debug.Log($"[JoinRoom] OnSubmit() - 入力されたコード: {code}");
        
        if (code.Length != 6)
        {
            Debug.LogWarning($"[JoinRoom] コード長エラー - 長さ: {code.Length}, 期待値: 6");
            messageText.text = "6桁の数字を入力してください";
            return;
        }
        
        roomCode = code;
        Debug.Log($"[JoinRoom] 部屋参加リクエスト開始 - roomCode: {roomCode}");
        StartCoroutine(JoinRoomRequest(code));
    }

    private IEnumerator JoinRoomRequest(string code)
    {
        Debug.Log($"[JoinRoom] JoinRoomRequest() - API呼び出し開始");
        
        var json = JsonUtility.ToJson(new JoinRoomRequestData { code = code, playerId = playerId });
        Debug.Log($"[JoinRoom] リクエストJSON: {json}");
        
        var request = new UnityWebRequest(joinUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[JoinRoom] JoinRoom API成功 - response: {request.downloadHandler.text}");
            var response = JsonUtility.FromJson<JoinRoomResponse>(request.downloadHandler.text);
            
            Debug.Log($"[JoinRoom] レスポンス解析 - message: {response.message}, player1Id: {response.player1Id}");
            
            if (response.message == "Matched successfully")
            {
                // マッチング成功
                Debug.Log($"[JoinRoom] 🎉 マッチング成立！opponentId: {response.player1Id}");
                isMatched = true;
                opponentId = response.player1Id; // 自分はplayer2
                
                // UIを更新
                ShowMatchedPanel();
                
                // ゲーム開始処理を共通クラスで実行
                Debug.Log($"[JoinRoom] ゲーム開始処理開始 - roomCode: {roomCode}, playerId: {playerId}, opponentId: {opponentId}");
                yield return StartCoroutine(
                    OnlineBattleStarter.StartGameAndTransition(
                        startGameUrl, roomCode, playerId, opponentId, false));
            }
            else
            {
                Debug.Log($"[JoinRoom] マッチング待ち状態 - message: {response.message}");
                messageText.text = "マッチング成功";
            }
        }
        else
        {
            Debug.LogError($"[JoinRoom] JoinRoom API失敗 - {request.error}");
            Debug.LogError($"[JoinRoom] エラーレスポンス: {request.downloadHandler.text}");
            messageText.text = "マッチング失敗: " + request.downloadHandler.text;
        }
    }

    /// <summary>
    /// マッチング完了パネルを表示
    /// </summary>
    private void ShowMatchedPanel()
    {
        Debug.Log($"[JoinRoom] ShowMatchedPanel() - マッチング完了パネル表示");
        if (inputPanel != null) inputPanel.SetActive(false);
        if (matchedPanel != null) matchedPanel.SetActive(true);
        if (matchedText != null) matchedText.text = "Matched! Starting game...";
    }

    [System.Serializable]
    private class JoinRoomRequestData
    {
        public string code;
        public string playerId;
    }

    [System.Serializable]
    private class JoinRoomResponse
    {
        public string message;
        public string player1Id;
        public string player2Id;
    }

    [System.Serializable]
    private class UserData
    {
        public string username;
        public string email;
        public string password;
    }
} 