using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

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
    
    private float currentTime;
    private bool isTimerRunning = false;
    
    // API GatewayのエンドポイントURL
    private string apiUrl = "https://s4sg7fzh7c.execute-api.ap-northeast-1.amazonaws.com/dev/create";
    
    void Start()
    {
        StartCoroutine(CreateRoom());
        
        // タイマーを初期化
        currentTime = countdownTime;
        UpdateTimerDisplay();
        
        // タイマーを開始
        StartTimer();
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
        string playerId = "guest";
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
            roomNumberText.text = response.code;
        }
        else
        {
            roomNumberText.text = "Error";
        }
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
} 