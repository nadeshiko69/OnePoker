using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class RegisterUI : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private TMP_InputField confirmationCodeInput;

    [Header("Buttons")]
    [SerializeField] private Button registerButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button backToLoginButton;

    [Header("Panels")]
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private GameObject errorPanel;

    [Header("Error Messages")]
    [SerializeField] private TextMeshProUGUI errorText;

    private string currentUsername;
    private string currentEmail;

    private void Start()
    {
        // 初期状態の設定
        confirmationPanel.SetActive(false);
        errorPanel.SetActive(false);

        // ボタンのリスナー設定
        registerButton.onClick.AddListener(OnRegisterButtonClicked);
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        backToLoginButton.onClick.AddListener(OnBackToLoginClicked);

        // 入力フィールドのバリデーション設定
        passwordInput.onValueChanged.AddListener(ValidatePassword);
        confirmPasswordInput.onValueChanged.AddListener(ValidatePassword);
    }

    private void ValidatePassword(string _)
    {
        bool passwordsMatch = passwordInput.text == confirmPasswordInput.text;
        bool passwordLengthValid = passwordInput.text.Length >= 8;
        registerButton.interactable = passwordsMatch && passwordLengthValid;
    }

    private async void OnRegisterButtonClicked()
    {
        if (!ValidateInputs())
        {
            return;
        }

        currentUsername = usernameInput.text;
        currentEmail = emailInput.text;

        bool success = await AuthManager.Instance.SignUpAsync(
            currentUsername,
            passwordInput.text,
            currentEmail
        );

        if (success)
        {
            registerPanel.SetActive(false);
            confirmationPanel.SetActive(true);
        }
        else
        {
            ShowError("登録に失敗しました。もう一度お試しください。");
        }
    }

    private async void OnConfirmButtonClicked()
    {
        if (string.IsNullOrEmpty(confirmationCodeInput.text))
        {
            ShowError("確認コードを入力してください。");
            return;
        }

        bool success = await AuthManager.Instance.ConfirmSignUpAsync(
            currentUsername,
            confirmationCodeInput.text
        );

        if (success)
        {
            // 登録完了後の処理（例：ログイン画面へ遷移）
            Debug.Log("登録が完了しました！");
            // TODO: ログイン画面への遷移処理を実装
        }
        else
        {
            ShowError("確認コードが無効です。もう一度お試しください。");
        }
    }

    private void OnBackToLoginClicked()
    {
        // TODO: ログイン画面への遷移処理を実装
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrEmpty(usernameInput.text))
        {
            ShowError("ユーザー名を入力してください。");
            return false;
        }

        if (string.IsNullOrEmpty(emailInput.text))
        {
            ShowError("メールアドレスを入力してください。");
            return false;
        }

        if (string.IsNullOrEmpty(passwordInput.text))
        {
            ShowError("パスワードを入力してください。");
            return false;
        }

        if (passwordInput.text != confirmPasswordInput.text)
        {
            ShowError("パスワードが一致しません。");
            return false;
        }

        if (passwordInput.text.Length < 8)
        {
            ShowError("パスワードは8文字以上で入力してください。");
            return false;
        }

        return true;
    }

    private void ShowError(string message)
    {
        errorText.text = message;
        errorPanel.SetActive(true);
    }

    public void HideError()
    {
        errorPanel.SetActive(false);
    }
} 