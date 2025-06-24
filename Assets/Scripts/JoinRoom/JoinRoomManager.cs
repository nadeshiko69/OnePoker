using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class JoinRoomManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputRoomCode;
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI messageText;

    private void Start()
    {
        submitButton.onClick.AddListener(OnSubmit);
    }

    private void OnSubmit()
    {
        string code = inputRoomCode.text;
        if (code.Length != 6)
        {
            messageText.text = "6桁の数字を入力してください";
            return;
        }
        StartCoroutine(JoinRoomRequest(code));
    }

    private IEnumerator JoinRoomRequest(string code)
    {
        // PlayerPrefsからUserData(JSON)を取得し、usernameをパース
        string userDataJson = PlayerPrefs.GetString("UserData", "");
        string playerId = "guest";
        if (!string.IsNullOrEmpty(userDataJson))
        {
            var userData = JsonUtility.FromJson<UserData>(userDataJson);
            playerId = userData.username;
        }
        string url = "https://s4sg7fzh7c.execute-api.ap-northeast-1.amazonaws.com/dev/join"; // API GatewayのURLを記載

        var json = JsonUtility.ToJson(new JoinRoomRequestData { code = code, playerId = playerId });
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            messageText.text = "マッチング成功";
            // 必要に応じてシーン遷移などの処理を追加
        }
        else
        {
            messageText.text = "マッチング失敗: " + request.downloadHandler.text;
        }
    }

    [System.Serializable]
    private class JoinRoomRequestData
    {
        public string code;
        public string playerId;
    }
} 