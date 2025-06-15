using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleManager : MonoBehaviour
{
    void Start(){
        registerAccountPanel.SetActive(false);
        confirmSignUpPanel.SetActive(false);
    }

    // アカウント登録
    public GameObject registerAccountPanel;
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public GameObject confirmSignUpPanel;
    
    // 確認コード入力用
    public TMP_InputField confirmationCodeInput;
    private AwsManager awsManager;

    void Awake()
    {
        awsManager = FindObjectOfType<AwsManager>();
    }
  
    // アカウント登録パネル操作
    public void OpenRegisterAccountPanel(){
        registerAccountPanel.SetActive(true);
    }
    public void CloseRegisterAccountPanel(){
        registerAccountPanel.SetActive(false);
    }
    public void OpenConfirmSignUpPanel(){
        confirmSignUpPanel.SetActive(true);
    }
    public void CloseConfirmSignUpPanel(){
        confirmSignUpPanel.SetActive(false);
    }

    // 確認コード送信
    public void OnConfirmCodeSubmit()
    {
        string username = usernameInput.text;
        string confirmationCode = confirmationCodeInput.text;

        if (string.IsNullOrEmpty(confirmationCode) || confirmationCode.Length != 6)
        {
            Debug.LogError("確認コードは6桁の数字を入力してください。");
            return;
        }
        awsManager.OnConfirmSignUpButtonClicked(username, confirmationCode);
        CloseConfirmSignUpPanel();
    }
}
