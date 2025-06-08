using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using System.Threading.Tasks;

public class SceneChange : MonoBehaviour
{
    private TitleManager titleManager;
    private AmazonCognitoIdentityProviderClient cognitoClient;

    void Start(){
        titleManager = FindObjectOfType<TitleManager>();
        // AWS Cognitoクライアントの初期化
        var credentials = new BasicAWSCredentials("YOUR_ACCESS_KEY", "YOUR_SECRET_KEY");
        cognitoClient = new AmazonCognitoIdentityProviderClient(credentials, Amazon.RegionEndpoint.APNortheast1);
    }

    // シーン切り替え
    public void ChangeRankMatchScene(){
        SceneManager.LoadScene("RankMatchScene");
    }

    public void ChangeCPUBattleScene(){
        SceneManager.LoadScene("CpuBattleScene");
    }

    public void ChangeSettingScene(){
        SceneManager.LoadScene("SettingScene");
    }

    // アカウント登録パネル操作
    public void OpenRegisterAccountPanel(){
        titleManager.registerAccountPanel.SetActive(true);
    }
    public void CloseRegisterAccountPanel(){
        titleManager.registerAccountPanel.SetActive(false);
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
            titleManager.ShowErrorMessage("パスワードの形式が正しくありません。");
        }
        catch (InvalidParameterException) {
            titleManager.ShowErrorMessage("入力内容が正しくありません。");
        }
        catch (System.Exception ex) {
            titleManager.ShowErrorMessage("登録に失敗しました: " + ex.Message);
        }
    }   
}