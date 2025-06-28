using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    private Vector2 friendMatchButtonOriginalPos;
    private bool friendMatchButtonOriginalInteractable;
    private List<GameObject> buttonsToHide = new List<GameObject>();
    private List<GameObject> buttonsToShow = new List<GameObject>();

    void Start(){
        registerAccountPanel.SetActive(false);
        confirmSignUpPanel.SetActive(false);
        userMenuPanel.SetActive(false);

        // ボタンを非表示にする
        if (createRoomButton != null) createRoomButton.SetActive(false);
        if (joinRoomButton != null) joinRoomButton.SetActive(false);

        // 元の位置と状態を記憶
        if (friendMatchButton != null)
        {
            friendMatchButtonOriginalPos = friendMatchButton.GetComponent<RectTransform>().anchoredPosition;
            friendMatchButtonOriginalInteractable = friendMatchButton.interactable;
        }
        // 非表示・表示切り替え対象をリスト化
        if (rankMatchButton != null) buttonsToShow.Add(rankMatchButton.gameObject);
        if (cpuBattleButton != null) buttonsToShow.Add(cpuBattleButton.gameObject);
        if (createRoomButton != null) buttonsToHide.Add(createRoomButton);
        if (joinRoomButton != null) buttonsToHide.Add(joinRoomButton);

        // if (AdMobManager.Instance != null)
        // {
        //     AdMobManager.Instance.ShowBannerAd();
        // }
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

    /// <summary>
    /// 画面状態をリセットする
    /// </summary>
    public void ResetMatchButtons()
    {
        // 移動したボタンを元の位置に戻し、有効化
        if (friendMatchButton != null)
        {
            var rect = friendMatchButton.GetComponent<RectTransform>();
            rect.anchoredPosition = friendMatchButtonOriginalPos;
            friendMatchButton.interactable = friendMatchButtonOriginalInteractable;
        }
        // 表示させたボタンを非表示
        foreach (var go in buttonsToHide)
        {
            if (go != null) go.SetActive(false);
        }
        // 非表示にしたボタンを表示
        foreach (var go in buttonsToShow)
        {
            if (go != null) go.SetActive(true);
        }
    }
}
