using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.SceneManagement;

public class CreateRoomManager : MonoBehaviour
{
    [Header("部屋管理")]
    [SerializeField] private TextMeshProUGUI roomNumberText;
    
    [Header("タイマー設定")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float countdownTime = 900f; // 15分 = 900秒
    
    [Header("タイマー表示設定")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private float warningThreshold = 300f; // 5分前から警告色
    [SerializeField] private float dangerThreshold = 60f; // 1分前から危険色
    
    [Header("マッチング状態")]
    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private GameObject matchedPanel;
    [SerializeField] private TextMeshProUGUI matchedText;
    
    private float currentTime;
    private bool isTimerRunning = false;
    private string roomCode;
    private string playerId;
    private string opponentId;
    private bool isMatched = false;
    
    // API GatewayのエンドポイントURL
    private string apiUrl = "https://lbr9cfjhuk.execute-api.ap-northeast-1.amazonaws.com/dev/create";
    private string checkMatchUrl = "https://lbr9cfjhuk.execute-api.ap-northeast-1.amazonaws.com/dev/check-match";
    private string startGameUrl = "https://5tp37snsbk.execute-api.ap-northeast-1.amazonaws.com/dev/start-game";
    
    void Start()
    {
        StartCoroutine(CreateRoom());
        
        // タイマーを初期化
        currentTime = countdownTime;
        UpdateTimerDisplay();
        
        // タイマーを開始
        StartTimer();
        
        // マッチング状態のチェックを開始
        StartCoroutine(CheckMatchingStatus());
    }
    
    void Update()
    {
        if (isTimerRunning)
        {
            // タイマーを更新
            currentTime -= Time.deltaTime;
            
            // タイマーが0になった場合
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                isTimerRunning = false;
                OnTimerComplete();
            }
            
            UpdateTimerDisplay();
        }
    }
    
    /// <summary>
    /// タイマーを開始します
    /// </summary>
    public void StartTimer()
    {
        isTimerRunning = true;
        currentTime = countdownTime;
    }
    
    /// <summary>
    /// タイマーを停止します
    /// </summary>
    public void StopTimer()
    {
        isTimerRunning = false;
    }
    
    /// <summary>
    /// タイマーをリセットします
    /// </summary>
    public void ResetTimer()
    {
        currentTime = countdownTime;
        UpdateTimerDisplay();
    }
    
    /// <summary>
    /// タイマーの表示を更新します
    /// </summary>
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            
            // 残り時間に応じて色を変更
            if (currentTime <= dangerThreshold)
            {
                timerText.color = dangerColor;
            }
            else if (currentTime <= warningThreshold)
            {
                timerText.color = warningColor;
            }
            else
            {
                timerText.color = normalColor;
            }
        }
    }
    
    /// <summary>
    /// タイマーが完了した時の処理
    /// </summary>
    private void OnTimerComplete()
    {
        Debug.Log("タイマーが完了しました");
        // ここにタイマー完了時の処理を追加
        // 例: ルームを自動で閉じる、警告を表示するなど
    }
    
    /// <summary>
    /// 残り時間を取得します（秒）
    /// </summary>
    public float GetRemainingTime()
    {
        return currentTime;
    }
    
    /// <summary>
    /// タイマーが実行中かどうかを取得します
    /// </summary>
    public bool IsTimerRunning()
    {
        return isTimerRunning;
    }

    private IEnumerator CreateRoom()
    {
        // PlayerPrefsからUserData(JSON)を取得し、usernameをパース
        string userDataJson = PlayerPrefs.GetString("UserData", "");
        playerId = "guest";
        if (!string.IsNullOrEmpty(userDataJson))
        {
            var userData = JsonUtility.FromJson<UserData>(userDataJson);
            playerId = userData.username;
        }
        string json = "{\"playerId\":\"" + playerId + "\"}";

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<RoomCodeResponse>(request.downloadHandler.text);
            roomCode = response.code;
            roomNumberText.text = roomCode;
        }
        else
        {
            roomNumberText.text = "Error";
        }
    }

    /// <summary>
    /// マッチング状態を定期的にチェック
    /// </summary>
    private IEnumerator CheckMatchingStatus()
    {
        while (!isMatched)
        {
            yield return new WaitForSeconds(2f); // 2秒間隔でチェック
            
            if (!string.IsNullOrEmpty(roomCode))
            {
                yield return StartCoroutine(CheckMatchStatus());
            }
        }
    }

    /// <summary>
    /// マッチング状態をチェック
    /// </summary>
    private IEnumerator CheckMatchStatus()
    {
        string url = $"{checkMatchUrl}?roomCode={roomCode}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<MatchStatusResponse>(request.downloadHandler.text);
                
                if (response.status == "matched")
                {
                    // マッチング完了
                    isMatched = true;
                    opponentId = response.guestPlayerId;
                    
                    // UIを更新
                    ShowMatchedPanel();
                    
                    // ゲーム開始処理を共通クラスで実行
                    yield return StartCoroutine(
                        OnlineBattleStarter.StartGameAndTransition(
                            startGameUrl, roomCode, playerId, opponentId, true));
                }
            }
        }
    }

    /// <summary>
    /// マッチング完了パネルを表示
    /// </summary>
    private void ShowMatchedPanel()
    {
        if (waitingPanel != null) waitingPanel.SetActive(false);
        if (matchedPanel != null) matchedPanel.SetActive(true);
        if (matchedText != null) matchedText.text = $"Matched with {opponentId}!";
        
        // タイマーを停止
        StopTimer();
    }

    [System.Serializable]
    private class RoomCodeResponse
    {
        public string code;
    }

    [System.Serializable]
    private class UserData
    {
        public string username;
        public string email;
        public string password;
    }

    [System.Serializable]
    private class MatchStatusResponse
    {
        public string status;
        public string guestPlayerId;
    }

    [System.Serializable]
    private class OnlineGameData
    {
        public string roomCode;
        public string playerId;
        public string opponentId;
        public bool isPlayer1;
        public string gameId;
    }
} 