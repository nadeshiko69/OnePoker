using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleManager : MonoBehaviour
{
    // アカウント登録
    public GameObject registerAccountPanel;
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public GameObject confirmSignUpPanel;

    // アカウントボタン
    public Button accountButton;
    public TextMeshProUGUI accountText;

    // ユーザーメニューパネル
    public GameObject userMenuPanel;
    public bool alreadyLogin = false;

    [Header("対戦ボタン管理")]
    [SerializeField] private TitleAnimationManager titleAnimationManager;
    [SerializeField] private GameObject createRoomButton;
    [SerializeField] private GameObject joinRoomButton;
    [SerializeField] private Button friendMatchButton;
    [SerializeField] private Button rankMatchButton;
    [SerializeField] private Button cpuBattleButton;

    void Start(){
        registerAccountPanel.SetActive(false);
        confirmSignUpPanel.SetActive(false);
        userMenuPanel.SetActive(false);

        // ボタンを非表示にする
        if (createRoomButton != null) createRoomButton.SetActive(false);
        if (joinRoomButton != null) joinRoomButton.SetActive(false);
    }
    
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

    // アカウントボタンクリック時の処理
    public void OnAccountButtonClicked()
    {
        // ユーザー名が表示されている場合（ログイン済み）
        if (alreadyLogin)
        {
            userMenuPanel.SetActive(true);
        }
        // 未ログインの場合
        else
        {
            OpenRegisterAccountPanel();
        }
    }

    public void CloseUserMenuPanel()
    {
        userMenuPanel.SetActive(false);
    }

    // FriendMatchボタン用のOnClickイベント
    public void OnClickFriendMatchButton()
    {
        if (titleAnimationManager != null && friendMatchButton != null)
        {
            titleAnimationManager.MoveButton(
                friendMatchButton.GetComponent<RectTransform>(),
                () =>
                {
                    // ボタンを無効化
                    friendMatchButton.interactable = false;
                    // 2つのボタンを表示
                    if (createRoomButton != null) createRoomButton.SetActive(true);
                    if (joinRoomButton != null) joinRoomButton.SetActive(true);
                    // 他の機能ボタンを非表示
                    if (rankMatchButton != null) rankMatchButton.gameObject.SetActive(false);
                    if (cpuBattleButton != null) cpuBattleButton.gameObject.SetActive(false);
                }
            );
        }
    }
}
