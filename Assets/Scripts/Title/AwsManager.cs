using UnityEngine;
using UnityEngine.UI;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using System.Threading.Tasks;
using System.IO;



[System.Serializable]
public class AWSConfig
{
    public string AccessKey;
    public string SecretKey;
    public string ClientId;
    public string Region;
    public string UserPoolId;
}

public class AwsManager : MonoBehaviour
{
    private TitleManager titleManager;
    private AmazonCognitoIdentityProviderClient cognitoClient;
    private const string CONFIG_FILE_PATH = "Assets/Config/aws-config.json";

    void Start(){
        InitializeCognitoClient();
        titleManager = FindObjectOfType<TitleManager>();
    }


    private void InitializeCognitoClient()
    {
        try
        {
            // 設定ファイルから認証情報を読み込む
            string configPath = Path.Combine(Application.dataPath, CONFIG_FILE_PATH.Replace("Assets/", ""));
            if (File.Exists(configPath))
            {
                string jsonContent = File.ReadAllText(configPath);
                AWSConfig config = JsonUtility.FromJson<AWSConfig>(jsonContent);
                
                var credentials = new BasicAWSCredentials(config.AccessKey, config.SecretKey);
                cognitoClient = new AmazonCognitoIdentityProviderClient(credentials, Amazon.RegionEndpoint.APNortheast1);
            }
            else
            {
                Debug.LogError("AWS設定ファイルが見つかりません: " + configPath);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Cognitoクライアントの初期化に失敗しました: " + ex.Message);
        }
    }

        // アカウント登録処理
    public async void RegisterAccount(){
        string username = titleManager.usernameInput.text;
        string email = titleManager.emailInput.text;
        string password = titleManager.passwordInput.text;

        // 入力値の検証
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) {
            titleManager.ShowErrorMessage("すべての項目を入力してください。");
            return;
        }

        if (password.Length < 8) {
            titleManager.ShowErrorMessage("パスワードは8文字以上で入力してください。");
            return;
        }

        if (!IsValidEmail(email)) {
            titleManager.ShowErrorMessage("有効なメールアドレスを入力してください。");
            return;
        }

        try {
            var signUpRequest = new SignUpRequest
            {
                ClientId = "YOUR_COGNITO_CLIENT_ID",
                Username = username,
                Password = password,
                UserAttributes = new System.Collections.Generic.List<AttributeType>
                {
                    new AttributeType
                    {
                        Name = "email",
                        Value = email
                    }
                }
            };

            var response = await cognitoClient.SignUpAsync(signUpRequest);
            
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK) {
                titleManager.ShowSuccessMessage();
                titleManager.CloseRegisterAccountPanel();
            }
        }
        catch (UsernameExistsException) {
            titleManager.ShowErrorMessage("このユーザー名は既に使用されています。");
        }
        catch (InvalidPasswordException) {
            titleManager.ShowErrorMessage("パスワードは8文字以上で、大文字、小文字、数字を含める必要があります。");
        }
        catch (InvalidParameterException) {
            titleManager.ShowErrorMessage("入力内容が正しくありません。");
        }
        catch (System.Exception ex) {
            Debug.LogError("登録エラー: " + ex.Message);
            titleManager.ShowErrorMessage("登録に失敗しました。しばらく経ってから再度お試しください。");
        }
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
