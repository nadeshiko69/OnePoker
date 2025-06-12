using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Collections;
using System;

[System.Serializable]
public class UserData
{
    public string username;
    public string email;
    public string password;
}

[System.Serializable]
public class AuthTokens
{
    public string accessToken;
    public string idToken;
    public string refreshToken;
}

[System.Serializable]
public class LoginResponse
{
    public string message;
    public AuthTokens tokens;
}

public class AwsManager : MonoBehaviour
{
    private TitleManager titleManager;
    private const string CONFIG_FILE_PATH = "Assets/Config/aws-config.json";
    private const string USER_DATA_KEY = "UserData";
    private const string AUTH_TOKENS_KEY = "AuthTokens";
    private AuthTokens currentTokens;

    void Start(){
        titleManager = FindObjectOfType<TitleManager>();
        LoadAndAutoLogin();
    }

    private void LoadAndAutoLogin()
    {
        string savedUserData = PlayerPrefs.GetString(USER_DATA_KEY, "");
        if (!string.IsNullOrEmpty(savedUserData))
        {
            UserData userData = JsonUtility.FromJson<UserData>(savedUserData);
            StartCoroutine(AutoLogin(userData));
        }
    }

    private IEnumerator AutoLogin(UserData userData)
    {
        var json = JsonUtility.ToJson(userData);
        var request = new UnityWebRequest("https://ik9lesw2oa.execute-api.ap-northeast-1.amazonaws.com/dev/login", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("自動ログイン成功: " + request.downloadHandler.text);
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            SaveAuthTokens(response.tokens);
            // ログイン成功時の処理（例：メイン画面への遷移など）
        }
        else
        {
            Debug.LogError("自動ログイン失敗: " + request.error + " / " + request.downloadHandler.text);
            // ログイン失敗時の処理（例：ログイン画面の表示など）
        }
    }

    // 外部から呼び出す用
    public void OnRegisterButtonClicked()
    {
        string username = titleManager.usernameInput.text;
        string email = titleManager.emailInput.text;
        string password = titleManager.passwordInput.text;
        StartCoroutine(RegisterUser(username, email, password));
    }

    IEnumerator RegisterUser(string username, string email, string password)
    {
        var userData = new UserData { username = username, email = email, password = password };
        var json = JsonUtility.ToJson(userData);
        var request = new UnityWebRequest("https://ik9lesw2oa.execute-api.ap-northeast-1.amazonaws.com/dev/register", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("登録成功: " + request.downloadHandler.text);
            // ユーザー情報を保存
            SaveUserData(userData);
        }
        else
        {
            Debug.LogError("登録失敗: " + request.error + " / " + request.downloadHandler.text);
        }
    }

    private void SaveUserData(UserData userData)
    {
        string json = JsonUtility.ToJson(userData);
        PlayerPrefs.SetString(USER_DATA_KEY, json);
        PlayerPrefs.Save();
    }

    private void SaveAuthTokens(AuthTokens tokens)
    {
        currentTokens = tokens;
        string json = JsonUtility.ToJson(tokens);
        PlayerPrefs.SetString(AUTH_TOKENS_KEY, json);
        PlayerPrefs.Save();
    }

    public void Logout()
    {
        currentTokens = null;
        PlayerPrefs.DeleteKey(AUTH_TOKENS_KEY);
        PlayerPrefs.DeleteKey(USER_DATA_KEY);
        PlayerPrefs.Save();
        // ログアウト後の処理（例：ログイン画面への遷移など）
    }

    public bool IsLoggedIn()
    {
        return currentTokens != null && !string.IsNullOrEmpty(currentTokens.accessToken);
    }

    private bool IsValidEmail(string email)
    {
        try {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch {
            return false;
        }
    }
}
