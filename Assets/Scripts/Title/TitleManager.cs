using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleManager : MonoBehaviour
{
    void Start(){
        registerAccountPanel.SetActive(false);
    }

    // アカウント登録
    public GameObject registerAccountPanel;
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public GameObject successMessage;
    public GameObject errorMessage;
    public TMP_Text errorText;

    public void ShowSuccessMessage() {
        successMessage.SetActive(true);
        errorMessage.SetActive(false);
    }

    public void ShowErrorMessage(string message) {
        errorMessage.SetActive(true);
        successMessage.SetActive(false);
        errorText.text = message;
    }
  
    // アカウント登録パネル操作
    public void OpenRegisterAccountPanel(){
        registerAccountPanel.SetActive(true);
    }
    public void CloseRegisterAccountPanel(){
        registerAccountPanel.SetActive(false);
    }
}
