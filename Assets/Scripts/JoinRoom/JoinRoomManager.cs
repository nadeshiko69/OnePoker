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
        submitButton.onClick.AddListener(OnSubmit);
        
        // PlayerPrefsからUserData(JSON)を取得し、usernameをパース
        string userDataJson = PlayerPrefs.GetString("UserData", "");
        playerId = "guest";
        if (!string.IsNullOrEmpty(userDataJson))
        {
            var userData = JsonUtility.FromJson<UserData>(userDataJson);
            playerId = userData.username;
        }
    }

    private void OnSubmit()
    {
        string code = inputRoomCode.text;
        if (code.Length != 6)
        {
            messageText.text = "6桁の数字を入力してください";
            return;
        }
        
        roomCode = code;
        StartCoroutine(JoinRoomRequest(code));
    }

    private IEnumerator JoinRoomRequest(string code)
    {
        var json = JsonUtility.ToJson(new JoinRoomRequestData { code = code, playerId = playerId });
        var request = new UnityWebRequest(joinUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<JoinRoomResponse>(request.downloadHandler.text);
            
            if (response.message == "Matched successfully")
            {
                // マッチング成功
                isMatched = true;
                opponentId = response.player1Id; // 自分はplayer2
                // UIを更新
                ShowMatchedPanel();
                // ゲーム開始処理を共通クラスで実行
                yield return StartCoroutine(
                    OnlineBattleStarter.StartGameAndTransition(
                        startGameUrl, roomCode, playerId, opponentId, false));
            }
            else
            {
                messageText.text = "マッチング成功";
            }
        }
        else
        {
            messageText.text = "マッチング失敗: " + request.downloadHandler.text;
        }
    }

    /// <summary>
    /// マッチング完了パネルを表示
    /// </summary>
    private void ShowMatchedPanel()
    {
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