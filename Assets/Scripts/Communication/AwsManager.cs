using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Collections;
using System;
using System.Collections.Generic;
using OnePoker.Network; // HttpManagerのnamespaceをインポート

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
        
        // For Debug
        // PlayerPrefs.DeleteAll();
        // PlayerPrefs.Save();     
        
        LoadAndAutoLogin();
    }

    private void LoadAndAutoLogin()
    {
        string savedUserData = PlayerPrefs.GetString(USER_DATA_KEY, "");
        if (!string.IsNullOrEmpty(savedUserData))
        {
            UserData userData = JsonUtility.FromJson<UserData>(savedUserData);
            CognitoLogin(userData.username, userData.password);
        }
    }

    // 外部から呼び出す用
    public void OnRegisterButtonClicked()
    {
        string username = titleManager.usernameInput.text;
        string email = titleManager.emailInput.text;
        string password = titleManager.passwordInput.text;
        RegisterUser(username, email, password);
    }

    void RegisterUser(string username, string email, string password)
    {
        string clientId = "hv3rji4sb8s5h6a9vmefj77r4";
        string endpoint = "https://cognito-idp.ap-northeast-1.amazonaws.com/";

        // 手動でJSON文字列を組み立て
        string jsonBody = $"{{\"ClientId\":\"{clientId}\",\"Username\":\"{username}\",\"Password\":\"{password}\",\"UserAttributes\":[{{\"Name\":\"email\",\"Value\":\"{email}\"}}]}}";

        var headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/x-amz-json-1.1" },
            { "X-Amz-Target", "AWSCognitoIdentityProviderService.SignUp" }
        };
        
        // 成功時のレスポンスは利用しないため、型はobjectで受ける
        HttpManager.Instance.Post<object>(endpoint, jsonBody, 
            response => {
                Debug.Log("Cognitoユーザー登録成功");
                // ユーザー情報を保存
                UserData userData = new UserData
                {
                    username = username,
                    email = email,
                    password = password // 注意: パスワードの平文保存は非推奨です
                };
                SaveUserData(userData);
                titleManager.OpenConfirmSignUpPanel();
            },
            error => {
                if (error.Contains("UsernameExistsException"))
                {
                    Debug.LogError("このユーザー名は既に使用されています。");
                }
                else
                {
                    Debug.LogError("Cognitoユーザー登録失敗: " + error);
                }
            },
            headers
        );
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

    public void OnConfirmSignUpButtonClicked(string username, string confirmationCode)
    {
        ConfirmSignUp(username, confirmationCode);
    }

    void ConfirmSignUp(string username, string confirmationCode)
    {
        string clientId = "hv3rji4sb8s5h6a9vmefj77r4";
        string endpoint = "https://cognito-idp.ap-northeast-1.amazonaws.com/";
        string jsonBody = $"{{\"ClientId\":\"{clientId}\",\"Username\":\"{username}\",\"ConfirmationCode\":\"{confirmationCode}\"}}";

        var headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/x-amz-json-1.1" },
            { "X-Amz-Target", "AWSCognitoIdentityProviderService.ConfirmSignUp" }
        };

        HttpManager.Instance.Post<object>(endpoint, jsonBody,
            response => {
                Debug.Log("メール認証成功");
                // 認証済みユーザーとして自動ログイン
                string savedUserData = PlayerPrefs.GetString(USER_DATA_KEY, "");
                if (!string.IsNullOrEmpty(savedUserData))
                {
                    UserData userData = JsonUtility.FromJson<UserData>(savedUserData);
                    CognitoLogin(userData.username, userData.password);
                }
            },
            error => {
                if (error.Contains("UserNotFoundException"))
                {
                    Debug.LogError("ユーザーが見つかりません。登録からやり直してください。");
                }
                else if (error.Contains("CodeMismatchException"))
                {
                    Debug.LogError("確認コードが正しくありません。");
                }
                else if (error.Contains("ExpiredCodeException"))
                {
                    Debug.LogError("確認コードの有効期限が切れています。新しいコードをリクエストしてください。");
                }
                else
                {
                    Debug.LogError("メール認証失敗: " + error);
                }
            },
            headers
        );
    }

    void CognitoLogin(string username, string password)
    {
        string clientId = "hv3rji4sb8s5h6a9vmefj77r4";
        string endpoint = "https://cognito-idp.ap-northeast-1.amazonaws.com/";
        string jsonBody = "{\"AuthParameters\":{\"USERNAME\":\"" + username + "\",\"PASSWORD\":\"" + password + "\"},\"AuthFlow\":\"USER_PASSWORD_AUTH\",\"ClientId\":\"" + clientId + "\"}";

        Debug.Log("CognitoLoginリクエスト: " + jsonBody);

        var headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/x-amz-json-1.1" },
            { "X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth" }
        };

        // Cognitoのレスポンスは複雑なため、一度JSON文字列で受けてから処理する方が安全かもしれないが、
        // ここではLoginResponseにマッピングしてみる
        HttpManager.Instance.Post<object>(endpoint, jsonBody,
            response => {
                Debug.Log("Cognitoログイン成功: " + response);
                // TODO: レスポンスをパースしてトークンを保存する処理
                // SaveAuthTokens(...)

                // ログイン成功時にボタンのテキストを変更
                if (titleManager != null && titleManager.accountButton != null)
                {
                    titleManager.accountText.text = username;
                    titleManager.alreadyLogin = true;
                }
            },
            error => {
                 Debug.LogError("Cognitoログイン失敗: " + error);
            },
            headers
        );
    }
}
