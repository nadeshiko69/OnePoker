using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class PanelManager : MonoBehaviour
{
    // スキルボタン
    public Button ScanSkillButton;
    public Button ChangeSkillButton;
    public Button ObstructSkillButton;
    public Button FakeOutSkillButton;
    public Button CopySkillButton;

    // セット確認用のUI
    public GameObject confirmationPanel;
    public Button yesButton;
    public Button noButton;

    // ベットフェーズ用のUI
    public GameObject bettingPanel;
    public Button betPlusButton;
    public Button betMinusButton;
    public Button callButton;
    public TextMeshProUGUI callButtonText;
    public Button dropButton;
    
    // オープンのUI
    public GameObject openPanel;

    // ドロップのUI
    public GameObject dropPanel;

    // 勝敗表示用のUI
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    private ResultViewManager resultViewManager;
    private GameManager gameManager;
    private MatchManager matchManager;
    private SkillManager skillManager;

    void Start()
    {
        resultViewManager = FindObjectOfType<ResultViewManager>();
        gameManager = FindObjectOfType<GameManager>();
        matchManager = FindObjectOfType<MatchManager>();
        skillManager = FindObjectOfType<SkillManager>();

        if (resultPanel != null) resultPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (bettingPanel != null) bettingPanel.SetActive(false);
        if (openPanel != null) openPanel.SetActive(false);
        if (dropPanel != null) dropPanel.SetActive(false);

        // ボタンUIの初期化
        yesButton.onClick.AddListener(gameManager.ConfirmPlacement);
        noButton.onClick.AddListener(gameManager.CancelPlacement);
        
        ShowSkillUI();
    }

    public void ShowSkillUI()
    {
        Debug.Log("ShowSkillUI called");
        ScanSkillButton.onClick.RemoveAllListeners();
        ChangeSkillButton.onClick.RemoveAllListeners();
        ObstructSkillButton.onClick.RemoveAllListeners();
        FakeOutSkillButton.onClick.RemoveAllListeners();
        CopySkillButton.onClick.RemoveAllListeners();

        ScanSkillButton.onClick.AddListener(() => skillManager.ScanSkill());
        ChangeSkillButton.onClick.AddListener(() => skillManager.ChangeSkill());
        ObstructSkillButton.onClick.AddListener(() => skillManager.ObstructSkill());
        FakeOutSkillButton.onClick.AddListener(() => skillManager.FakeOutSkill());
        CopySkillButton.onClick.AddListener(() => skillManager.CopySkill());

        VisibleSkillButtons(true);
        VisibleBetButtons(false);
    }

    public void ShowBettingUI()
    {
        bettingPanel.SetActive(true);
        StartCoroutine(HideBettingUI());

        // ベット開始時に自動で1をベット
        gameManager.PlaceBet(1);

        betPlusButton.onClick.RemoveAllListeners();
        betMinusButton.onClick.RemoveAllListeners();
        callButton.onClick.RemoveAllListeners();
        dropButton.onClick.RemoveAllListeners();

        betPlusButton.onClick.AddListener(() => gameManager.PlaceBet(1));
        betMinusButton.onClick.AddListener(() => gameManager.PlaceBet(-1));
        callButton.onClick.AddListener(() => StartCoroutine(HandleCall()));
        dropButton.onClick.AddListener(() => StartCoroutine(HandleDrop()));

        // ベットフェーズでベットボタンを有効化
        VisibleSkillButtons(false);
        VisibleBetButtons(true);
    }

    // 勝敗判定を行い結果を表示
    public void ShowResultPanel(int SetPlayerCard, int SetOpponentCard)
    {
        if (resultPanel != null && resultText != null)
        {
            resultPanel.SetActive(true);

            if (SetPlayerCard == SetOpponentCard)
            {
                resultText.text = "DRAW";
                resultText.color = Color.white;
            }
            else if (resultViewManager.IsWinner(SetPlayerCard, SetOpponentCard))
            {
                resultText.text = "YOU WIN!";
                resultText.color = Color.red;
            }
            else
            {
                resultText.text = "YOU LOSE...";
                resultText.color = Color.blue;
            }

            Debug.Log($"Battle Result - Player: {SetPlayerCard} vs Opponent: {SetOpponentCard}");
        }
    }

    public void UpdateCallButtonText()
    {
        if (callButtonText != null)
        {
            string action = gameManager.CurrentBetAmount >= 2 ? "Raise" : "Call";
            callButtonText.text = $"{action} ({gameManager.CurrentBetAmount})";
        }
    }

    // 結果表示を非表示にする
    public void HidePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private IEnumerator HideBettingUI()
    {
        yield return new WaitForSeconds(1f);
        HidePanel(bettingPanel);
    }

    
    private IEnumerator HandleCall()
    {
        // プレイヤーがコール
        Debug.Log($"Player calls with {gameManager.CurrentBetAmount} life!");
        bettingPanel.SetActive(false);
        UpdateCallButtonText();

        yield return new WaitForSeconds(1f);

        // CPUがコール
        Debug.Log("Opponent calls!");
        gameManager.SetOpponentCalled(true);
        //　CPUのライフを更新する
        matchManager.UpdateOpponentLife(-gameManager.CurrentBetAmount);

        yield return new WaitForSeconds(1f);

        // オープンのUIを表示しカードオープン
        openPanel.SetActive(true);
        yield return new WaitForSeconds(1f);
        openPanel.SetActive(false);
        gameManager.RevealCards();
    }

    private IEnumerator HandleDrop(){
        // ドロップのUIを表示
        bettingPanel.SetActive(false);
        dropPanel.SetActive(true);
        yield return new WaitForSeconds(1f);
        dropPanel.SetActive(false);

        // 負けUIの表示（相手のDropを考慮して後々修正）
        resultPanel.SetActive(true);
        resultText.text = "YOU LOSE...";
        resultText.color = Color.blue;
        yield return new WaitForSeconds(1f);
        resultPanel.SetActive(false);

        // 相手勝ちとしてライフ更新
        matchManager.UpdateOpponentLife(gameManager.CurrentBetAmount);
        resultViewManager.UpdateResultTable(gameManager.SetPlayerCard.GetComponent<CardDisplay>().CardValue, gameManager.SetOpponentCard.GetComponent<CardDisplay>().CardValue, false, false);
        matchManager.OnGameComplete();
    }

    // ベットボタンの表示/非表示
    public void VisibleBetButtons(bool visible)
    {
        if (callButton != null) callButton.gameObject.SetActive(visible);
        if (dropButton != null) dropButton.gameObject.SetActive(visible);
        if (betPlusButton != null) betPlusButton.gameObject.SetActive(visible);
        if (betMinusButton != null) betMinusButton.gameObject.SetActive(visible);
    }

    // スキルボタンの表示/非表示
    public void VisibleSkillButtons(bool visible)
    {
        if (ScanSkillButton != null) ScanSkillButton.gameObject.SetActive(visible);
        if (ChangeSkillButton != null) ChangeSkillButton.gameObject.SetActive(visible);
        if (ObstructSkillButton != null) ObstructSkillButton.gameObject.SetActive(visible);
        if (FakeOutSkillButton != null) FakeOutSkillButton.gameObject.SetActive(visible);
        if (CopySkillButton != null) CopySkillButton.gameObject.SetActive(visible);
    }   
}
