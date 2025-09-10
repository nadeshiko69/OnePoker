using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class OnlinePanelManager : MonoBehaviour
{
    [Header("Phase Display")]
    public TextMeshProUGUI phaseText;
    
    [Header("プレイヤーロール表示")]
    public TextMeshProUGUI playerRoleText;
    public TextMeshProUGUI opponentRoleText;
    

    [Header("◎　Start Match")]
    public GameObject matchStartPanel;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI opponentNameText;
    // TODO: マッチング開始パネルにランク表示を追加
    public TextMeshProUGUI playerRate;
    public TextMeshProUGUI opponentRate;

    public TextMeshProUGUI playerLife;
    public TextMeshProUGUI opponentLife;

    [Header("◎　Phase通知用UI")]
    public GameObject startPhasePanel;
    public TextMeshProUGUI startPhaseTitle;
    public TextMeshProUGUI startPhaseDescription;

    [Header("◎　Set Phase")]
    [Header("スキルボタン")]
    public Button ScanSkillButton;
    public Button ChangeSkillButton;
    public Button ObstructSkillButton;
    public Button FakeOutSkillButton;
    public Button CopySkillButton;
    public Button ChangeCard1Button;
    public Button ChangeCard2Button;

    [Header("スキル確認パネル")]
    public GameObject descriptionSkillPanel;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescriptionText;
    public Button UseSkillButton;
    public Button CancelSkillButton;
    public GameObject obstructPanel;

    private string descriptionScanSkill = "相手の手札を\n1枚ランダムに確認できます。";
    private string descriptionChangeSkill = "自分の手札を1枚捨て、\n山札から1枚引きます。";
    private string descriptionObstructSkill = "次のターン、相手がスキルを\n使用できなくなります。";
    private string descriptionFakeOutSkill = "相手にScanを使用したと\n通知しますが、実際に見ることは\nできません（ブラフ用）";
    private string descriptionCopySkill = "前のターンに相手が使用した\nスキルを使用できます。";
    public string DescriptionScanSkill => descriptionScanSkill;
    public string DescriptionChangeSkill => descriptionChangeSkill;
    public string DescriptionObstructSkill => descriptionObstructSkill;
    public string DescriptionFakeOutSkill => descriptionFakeOutSkill;
    public string DescriptionCopySkill => descriptionCopySkill;

    [Header("セット確認パネル")]
    public GameObject confirmationPanel;
    public Button yesButton;
    public Button noButton;

    [Header("セット完了通知パネル")]
    public GameObject setCompletePanel;
    public TextMeshProUGUI setCompleteText;

    [Header("◎　Betting Phase")]
    [Header("ベットパネル")]
    public GameObject bettingPanel;
    [Header("ベット値設定ボタン")]
    public Button betPlusButton;
    public Button betMinusButton;
    public Button callButton;
    public TextMeshProUGUI callButtonText;
    public Button dropButton;
    
    [Header("ベット額表示")]
    public TextMeshProUGUI betAmountText;
    public TextMeshProUGUI OpponentBetAmountText;
    
    [Header("親プレイヤーBet中パネル")]
    public GameObject parentBettingPanel;
    
    [Header("HIGH/LOW表示")]
    public TextMeshProUGUI opponent_updown1;
    public TextMeshProUGUI opponent_updown2;
    public TextMeshProUGUI player_updown1;
    public TextMeshProUGUI player_updown2;
    
    [Header("相手アクション通知パネル")]
    public GameObject opponentActionPanel;
    public TextMeshProUGUI opponentActionText;

    [Header("◎　Open Phase")]
    public GameObject openPanel;
    public GameObject dropPanel;
    public GameObject gameResultPanel;
    public TextMeshProUGUI gameResultText;
    public GameObject matchResultPanel;
    public TextMeshProUGUI matchResultText;
    public Button matchRestartButton;

    private OnlineResultViewManager resultViewManager;
    private OnlineGameManager gameManager;
    private OnlineMatchManager matchManager;
    private OnlineSkillManager skillManager;
    private CardDisplay cardDisplay;

    void Start()
    {
        Debug.Log("OnlinePanelManager.Start() called");
        
        resultViewManager = FindObjectOfType<OnlineResultViewManager>();
        gameManager = FindObjectOfType<OnlineGameManager>();
        matchManager = FindObjectOfType<OnlineMatchManager>();
        skillManager = FindObjectOfType<OnlineSkillManager>();

        Debug.Log($"OnlinePanelManager - Managers found: resultViewManager={resultViewManager != null}, gameManager={gameManager != null}, matchManager={matchManager != null}, skillManager={skillManager != null}");

        if (gameResultPanel != null) gameResultPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (bettingPanel != null) bettingPanel.SetActive(false);
        if (openPanel != null) openPanel.SetActive(false);
        if (dropPanel != null) dropPanel.SetActive(false);
        if (descriptionSkillPanel != null) descriptionSkillPanel.SetActive(false);
        if (obstructPanel != null) obstructPanel.SetActive(false);
        if (matchStartPanel != null) matchStartPanel.SetActive(false);
        if (startPhasePanel != null) startPhasePanel.SetActive(false);
        if (setCompletePanel != null) setCompletePanel.SetActive(false);
        if (matchResultPanel != null) matchResultPanel.SetActive(false);
        if (opponentActionPanel != null) opponentActionPanel.SetActive(false);
        if (parentBettingPanel != null) parentBettingPanel.SetActive(false);
        if (betAmountText != null) betAmountText.gameObject.SetActive(false);
        if (OpponentBetAmountText != null) OpponentBetAmountText.gameObject.SetActive(false);
        // ボタンUIの初期化
        if (yesButton != null && gameManager != null)
        {
            yesButton.onClick.AddListener(gameManager.ConfirmPlacement);
            Debug.Log("OnlinePanelManager - yesButton listener added");
        }
        else
        {
            Debug.LogError($"OnlinePanelManager - yesButton or gameManager is null: yesButton={yesButton != null}, gameManager={gameManager != null}");
        }
        
        if (noButton != null && gameManager != null)
        {
            noButton.onClick.AddListener(gameManager.CancelPlacement);
            Debug.Log("OnlinePanelManager - noButton listener added");
        }
        else
        {
            Debug.LogError($"OnlinePanelManager - noButton or gameManager is null: noButton={noButton != null}, gameManager={gameManager != null}");
        }

        // ベットボタンのイベント設定
        if (betPlusButton != null && gameManager != null)
        {
            betPlusButton.onClick.AddListener(gameManager.IncreaseBetValue);
            Debug.Log("OnlinePanelManager - betPlusButton listener added");
        }
        
        if (betMinusButton != null && gameManager != null)
        {
            betMinusButton.onClick.AddListener(gameManager.DecreaseBetValue);
            Debug.Log("OnlinePanelManager - betMinusButton listener added");
        }
        
        if (callButton != null && gameManager != null)
        {
            callButton.onClick.AddListener(gameManager.CallOrRaise);
            Debug.Log("OnlinePanelManager - callButton listener added");
        }
        
        if (dropButton != null && gameManager != null)
        {
            dropButton.onClick.AddListener(gameManager.Drop);
            Debug.Log("OnlinePanelManager - dropButton listener added");
        }
        
        // 初期化時のボタン表示/非表示設定
        VisibleSkillButtons(true);      // スキルボタンを表示
        VisibleBetButtons(false);       // ベット設定ボタンを非表示
        VisibleChangeCardButtons(false); // カード変更ボタンを非表示
        
        ShowSkillUI();
        Debug.Log("OnlinePanelManager.Start() completed");
    }

    public void ShowSkillUI()
    {
        Debug.Log("ShowSkillUI called");
        ScanSkillButton.onClick.RemoveAllListeners();
        ChangeSkillButton.onClick.RemoveAllListeners();
        ObstructSkillButton.onClick.RemoveAllListeners();
        FakeOutSkillButton.onClick.RemoveAllListeners();
        CopySkillButton.onClick.RemoveAllListeners();

        ScanSkillButton.onClick.AddListener(() => ShowDescriptionSkillPanel(OnlineGameManager.SkillType.Scan, DescriptionScanSkill));
        ChangeSkillButton.onClick.AddListener(() => ShowDescriptionSkillPanel(OnlineGameManager.SkillType.Change, DescriptionChangeSkill));
        ObstructSkillButton.onClick.AddListener(() => ShowDescriptionSkillPanel(OnlineGameManager.SkillType.Obstruct, DescriptionObstructSkill));
        FakeOutSkillButton.onClick.AddListener(() => ShowDescriptionSkillPanel(OnlineGameManager.SkillType.FakeOut, DescriptionFakeOutSkill));
        CopySkillButton.onClick.AddListener(() => ShowDescriptionSkillPanel(OnlineGameManager.SkillType.Copy, DescriptionCopySkill));

        // プレイヤーがObstructスキルの被害を受けている場合はPanelを表示する
        if(skillManager.IsPlayerObstructed)obstructPanel.SetActive(true);
        skillManager.SetPlayerObstructed(false);

        SetSkillButtonInteractable(true);
        VisibleSkillButtons(true);
        SetBettingButtonInteractable(true);
        VisibleBetButtons(false);
        VisibleChangeCardButtons(false);
    }

    public void ShowBettingUI()
    {
        bettingPanel.SetActive(true);
        StartCoroutine(HideBettingUI());

        // ベット開始時に初期値を設定
        // 新しい実装では、Start()で既にボタンイベントが設定されているため、
        // ここでは初期化のみ行う

        // ObstructスキルのPanel出てたら消す
        if (obstructPanel != null) obstructPanel.SetActive(false);

        // ベットフェーズでベットボタンを有効化
        SetSkillButtonInteractable(true);
        VisibleSkillButtons(false);
        SetBettingButtonInteractable(true);
        VisibleBetButtons(true);
    }

    // 勝敗判定を行い結果を表示
    public void ShowGameResultPanel(int SetPlayerCard, int SetOpponentCard)
    {
        if (gameResultPanel != null && gameResultText != null)
        {
            gameResultPanel.SetActive(true);

            if (SetPlayerCard == SetOpponentCard)
            {
                gameResultText.text = "DRAW";
                gameResultText.color = Color.white;
            }
            else if (resultViewManager.IsWinner(SetPlayerCard, SetOpponentCard))
            {
                gameResultText.text = "YOU WIN!";
                gameResultText.color = Color.red;
            }
            else
            {
                gameResultText.text = "YOU LOSE...";
                gameResultText.color = Color.blue;
            }

            Debug.Log($"Battle Result - Player: {SetPlayerCard} vs Opponent: {SetOpponentCard}");
        }
    }

    public void UpdateCallButtonText(int betValue)
    {
        if (callButtonText != null)
        {
            if (betValue == 1)
            {
                callButtonText.text = "Call";
            }
            else
            {
                callButtonText.text = "Raise";
            }
            Debug.Log($"OnlinePanelManager - Call button text updated to: {callButtonText.text} (betValue: {betValue})");
        }
    }

    // ベット額表示の更新
    public void UpdateBetAmountDisplay(int betAmount)
    {
        if (betAmountText != null)
        {
            betAmountText.text = $"BET : {betAmount}";
            Debug.Log($"Bet amount display updated to: {betAmount}");
        }
        else
        {
            Debug.LogWarning("betAmountText is null! Please assign it in the Inspector.");
        }
    }

    // プレイヤーロール表示の更新
    public void UpdatePlayerRoleDisplay(bool isParent)
    {
        if (playerRoleText != null && opponentRoleText != null)
        {
            if (isParent)
            {
                playerRoleText.text = "Dealer";
                opponentRoleText.text = "Player";
            }
            else
            {
                playerRoleText.text = "Player";
                opponentRoleText.text = "Dealer";
            }
        }
    }

    // HIGH/LOW判定メソッド
    public string GetCardHighLow(int cardValue)
    {
        Debug.Log($"=== GetCardHighLow調査開始 ===");
        Debug.Log($"入力されたカード値: {cardValue}");
        
        // CardDisplay.csと同じ計算方法を使用
        // cardValue % 13: 0=2, 1=3, 2=4, ..., 11=K, 12=A
        int rankIndex = cardValue % 13;
        
        // ランクインデックスから実際の数字を取得
        // 0-5: 2,3,4,5,6,7 (LOW)
        // 6-12: 8,9,10,J,Q,K,A (HIGH)
        if (rankIndex >= 0 && rankIndex <= 5)
        {
            // 2,3,4,5,6,7 → LOW
            Debug.Log($"GetCardHighLow: カード値{cardValue} -> ランク{rankIndex} -> 数字{rankIndex + 2} -> LOW");
            return "LOW";
        }
        else
        {
            // 8,9,10,J,Q,K,A → HIGH
            Debug.Log($"GetCardHighLow: カード値{cardValue} -> ランク{rankIndex} -> 数字{(rankIndex == 12 ? "A" : (rankIndex + 2).ToString())} -> HIGH");
            return "HIGH";
        }
    }

    // プレイヤーのHIGH/LOW表示を更新
    public void UpdatePlayerHighLowDisplay(int card1Value, int card2Value)
    {
        Debug.Log($"=== UpdatePlayerHighLowDisplay開始 ===");
        Debug.Log($"入力値: card1Value={card1Value}, card2Value={card2Value}");
        // Debug.Log($"card1Valueの型: {card1Value.GetType()}");
        // Debug.Log($"card2Valueの型: {card2Value.GetType()}");
        
        if (player_updown1 != null && player_updown2 != null)
        {
            // Debug.Log($"UI要素の状態: player_updown1={player_updown1 != null}, player_updown2={player_updown2 != null}");
            
            string card1HighLow = GetCardHighLow(card1Value);
            string card2HighLow = GetCardHighLow(card2Value);
            
            Debug.Log($"HIGH/LOW判定結果: card1={card1Value}->{card1HighLow}, card2={card2Value}->{card2HighLow}");
            
            // LOW/HIGHの組み合わせは表示しない
            if (card1HighLow == "LOW" && card2HighLow == "HIGH")
            {
                card1HighLow = "HIGH";
                card2HighLow = "LOW";
            }

            player_updown1.text = card1HighLow;
            player_updown2.text = card2HighLow;
        }
        else
        {
            Debug.LogError($"UI要素がnull: player_updown1={player_updown1 != null}, player_updown2={player_updown2 != null}");
        }
        Debug.Log($"=== UpdatePlayerHighLowDisplay完了 ===");
    }

    // 相手プレイヤーのHIGH/LOW表示を更新
    public void UpdateOpponentHighLowDisplay(int card1Value, int card2Value)
    {
        Debug.Log($"=== UpdateOpponentHighLowDisplay開始 ===");
        Debug.Log($"入力値: card1Value={card1Value}, card2Value={card2Value}");
        Debug.Log($"card1Valueの型: {card1Value.GetType()}");
        Debug.Log($"card2Valueの型: {card2Value.GetType()}");
        
        if (opponent_updown1 != null && opponent_updown2 != null)
        {
            // Debug.Log($"UI要素の状態: opponent_updown1={opponent_updown1 != null}, opponent_updown2={opponent_updown2 != null}");
            
            string card1HighLow = GetCardHighLow(card1Value);
            string card2HighLow = GetCardHighLow(card2Value);
            
            Debug.Log($"HIGH/LOW判定結果: card1={card1Value}->{card1HighLow}, card2={card2Value}->{card2HighLow}");
            
            // LOW/HIGHの組み合わせは表示しない
            if (card1HighLow == "LOW" && card2HighLow == "HIGH")
            {
                card1HighLow = "HIGH";
                card2HighLow = "LOW";
            }
            opponent_updown1.text = card1HighLow;
            opponent_updown2.text = card2HighLow;
        }
        else
        {
            Debug.LogError($"UI要素がnull: opponent_updown1={opponent_updown1 != null}, opponent_updown2={opponent_updown2 != null}");
        }
        Debug.Log($"=== UpdateOpponentHighLowDisplay完了 ===");
    }

    // HIGH/LOW表示をクリア
    public void ClearHighLowDisplay()
    {
        Debug.Log("=== ClearHighLowDisplay実行 ===");
        // Debug.Log($"クリア前の状態:");
        // Debug.Log($"  player_updown1.text = '{player_updown1?.text ?? "null"}'");
        // Debug.Log($"  player_updown2.text = '{player_updown2?.text ?? "null"}'");
        // Debug.Log($"  opponent_updown1.text = '{opponent_updown1?.text ?? "null"}'");
        // Debug.Log($"  opponent_updown2.text = '{opponent_updown2?.text ?? "null"}'");
        
        if (player_updown1 != null) player_updown1.text = "";
        if (player_updown2 != null) player_updown2.text = "";
        if (opponent_updown1 != null) opponent_updown1.text = "";
        if (opponent_updown2 != null) opponent_updown2.text = "";
        
        // Debug.Log($"クリア後の状態:");
        // Debug.Log($"  player_updown1.text = '{player_updown1?.text ?? "null"}'");
        // Debug.Log($"  player_updown2.text = '{player_updown2?.text ?? "null"}'");
        // Debug.Log($"  opponent_updown1.text = '{opponent_updown1?.text ?? "null"}'");
        // Debug.Log($"  opponent_updown2.text = '{opponent_updown2?.text ?? "null"}'");
        Debug.Log("=== ClearHighLowDisplay完了 ===");
    }

    // 親のターンパネル表示
    public void ShowParentTurnPanel()
    {
        if (startPhasePanel != null)
        {
            startPhasePanel.SetActive(true);
            if (startPhaseTitle != null) startPhaseTitle.text = "親のターン";
            if (startPhaseDescription != null) startPhaseDescription.text = "ベット額を設定してください";
            Debug.Log("OnlinePanelManager - Parent turn panel shown");
        }

        // 親のターン時にbetAmountTextを表示
        if (betAmountText != null)
        {
            betAmountText.gameObject.SetActive(true);
            Debug.Log("OnlinePanelManager - betAmountText activated for parent turn");
        }
    }

    // 親のアクション待ちパネル表示
    public void ShowWaitingForParentPanel()
    {
        if (startPhasePanel != null)
        {
            startPhasePanel.SetActive(true);
            if (startPhaseTitle != null) startPhaseTitle.text = "親のアクション待ち";
            if (startPhaseDescription != null) startPhaseDescription.text = "親プレイヤーがベット中です...";
            Debug.Log("OnlinePanelManager - Waiting for parent panel shown");
        }
        
        // 親のアクション待ち時はbetAmountTextを非表示にする
        if (betAmountText != null)
        {
            betAmountText.gameObject.SetActive(false);
            Debug.Log("OnlinePanelManager - betAmountText hidden while waiting for parent");
        }
    }

    // 親のベット完了通知パネル表示
    public void ShowParentBetCompletePanel()
    {
        if (startPhasePanel != null)
        {
            startPhasePanel.SetActive(true);
            if (startPhaseTitle != null) startPhaseTitle.text = "Bet Phase";
            if (startPhaseDescription != null) startPhaseDescription.text = "DEALERのベット完了しました。\nベット額を設定してください。";
            Debug.Log("OnlinePanelManager - Parent bet complete panel shown");
        }
    }

    // 子のアクション待ちパネル表示
    public void ShowWaitingForChildPanel()
    {
        if (startPhasePanel != null)
        {
            startPhasePanel.SetActive(true);
            if (startPhaseTitle != null) startPhaseTitle.text = "Bet Phase";
            if (startPhaseDescription != null) startPhaseDescription.text = "PLAYERがBet値を設定中です";
            Debug.Log("OnlinePanelManager - Waiting for child panel shown");
        }
    }

    // 親のアクション待ちパネルを非表示
    public void HideWaitingForParentPanel()
    {
        if (startPhasePanel != null)
        {
            startPhasePanel.SetActive(false);
            Debug.Log("OnlinePanelManager - Waiting for parent panel hidden");
        }
        
        // 親プレイヤーBet中パネルも非表示
        if (parentBettingPanel != null)
        {
            parentBettingPanel.SetActive(false);
            Debug.Log("OnlinePanelManager - Parent betting panel hidden");
        }
    }
    
    // 親プレイヤーBet中パネルを表示
    public void ShowParentBettingPanel()
    {
        if (parentBettingPanel != null)
        {
            parentBettingPanel.SetActive(true);
            Debug.Log("OnlinePanelManager - Parent betting panel shown");
        }
        else
        {
            Debug.LogWarning("OnlinePanelManager - parentBettingPanel is null");
        }
    }
    
    // 親プレイヤーBet中パネルを非表示
    public void HideParentBettingPanel()
    {
        if (parentBettingPanel != null)
        {
            parentBettingPanel.SetActive(false);
            Debug.Log("OnlinePanelManager - Parent betting panel hidden");
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

    public void ShowMatchStartPanel(string player1, string player2, float duration)
    {
        if (matchStartPanel != null && playerNameText != null && opponentNameText != null)
        {
            playerNameText.text = player1;
            opponentNameText.text = player2;
            matchStartPanel.SetActive(true);
            StartCoroutine(HideMatchStartPanelAfterDelay(duration));
        }
    }

    private IEnumerator HideMatchStartPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (matchStartPanel != null)
            matchStartPanel.SetActive(false);
    }
    
    // パネルの状態をリセット
    public void ResetPanelState()
    {
        VisibleBetButtons(false);
        VisibleSkillButtons(true);
        VisibleChangeCardButtons(false);
        SetSkillButtonInteractable(true);
        SetBettingButtonInteractable(true);
        obstructPanel.SetActive(false);
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

    public void VisibleSkillSelectButtons(bool visible)
    {
        if (UseSkillButton != null) UseSkillButton.gameObject.SetActive(visible);
        if (CancelSkillButton != null) CancelSkillButton.gameObject.SetActive(visible);
    }

    public void VisibleChangeCardButtons(bool visible)
    {
        if (ChangeCard1Button != null) ChangeCard1Button.gameObject.SetActive(visible);
        if (ChangeCard2Button != null) ChangeCard2Button.gameObject.SetActive(visible);
    }

    public void ShowDescriptionSkillPanel(OnlineGameManager.SkillType skillType, string skillDescription)
    {
        descriptionSkillPanel.SetActive(true);
        skillNameText.text = skillType.ToString();
        skillDescriptionText.text = skillDescription;
        UseSkillButton.onClick.RemoveAllListeners();
        CancelSkillButton.onClick.RemoveAllListeners();

        UseSkillButton.onClick.AddListener(() => skillManager.UseSkill(skillType));
        CancelSkillButton.onClick.AddListener(() => descriptionSkillPanel.SetActive(false));
    }

    public void SetSkillButtonInteractable(bool interactable)
    {
        if (!skillManager.IsPlayerObstructed)
        {
            if (gameManager.PlayerCanUseScanSkill) ScanSkillButton.interactable = interactable;
            if (gameManager.PlayerCanUseChangeSkill) ChangeSkillButton.interactable = interactable;
            if (gameManager.PlayerCanUseObstructSkill) ObstructSkillButton.interactable = interactable;
            if (gameManager.PlayerCanUseFakeOutSkill) FakeOutSkillButton.interactable = interactable;
            if (gameManager.PlayerCanUseCopySkill) CopySkillButton.interactable = interactable;
        }
        else
        {
            ScanSkillButton.interactable = false;
            ChangeSkillButton.interactable = false; 
            ObstructSkillButton.interactable = false;
            FakeOutSkillButton.interactable = false;
            CopySkillButton.interactable = false;
        }
    }

    public void SetBettingButtonInteractable(bool interactable)
    {
        if (betPlusButton != null) betPlusButton.interactable = interactable;
        if (betMinusButton != null) betMinusButton.interactable = interactable;
        if (callButton != null) callButton.interactable = interactable;
        if (dropButton != null) dropButton.interactable = interactable;
    }

    // Set Phaseパネルの表示/非表示
    public void ShowStartPhasePanel(string title = "Set Phase", string description = "カードをSetZoneにセットしてください", float autoHideDelay = 3f)
    {
        if (startPhasePanel != null)
        {
            startPhasePanel.SetActive(true);
            
            // タイトルとテキストを設定
            if (startPhaseTitle != null)
            {
                startPhaseTitle.text = title;
                Debug.Log($"OnlinePanelManager - Set startPhaseTitle to: '{title}'");
            }
            else
            {
                Debug.LogWarning("OnlinePanelManager - startPhaseTitle is null!");
            }
            
            if (startPhaseDescription != null)
            {
                startPhaseDescription.text = description;
                Debug.Log($"OnlinePanelManager - Set startPhaseDescription to: '{description}'");
            }
            else
            {
                Debug.LogWarning("OnlinePanelManager - startPhaseDescription is null!");
            }
            
            Debug.Log($"OnlinePanelManager - StartPhasePanel shown with title: '{title}', description: '{description}', autoHideDelay: {autoHideDelay}");
            
            // 指定された時間後に自動的に非表示にする
            if (autoHideDelay > 0)
            {
                StartCoroutine(AutoHideStartPhasePanelAfterDelay(autoHideDelay));
            }
        }
        else
        {
            Debug.LogError("OnlinePanelManager - startPhasePanel is null!");
        }
    }

    // StartPhasePanelを指定時間後に非表示にするコルーチン
    private IEnumerator AutoHideStartPhasePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (startPhasePanel != null)
        {
            startPhasePanel.SetActive(false);
            Debug.Log($"OnlinePanelManager - StartPhasePanel auto-hidden after {delay} seconds");
        }
    }

    public void HideStartPhasePanel()
    {
        if (startPhasePanel != null)
        {
            startPhasePanel.SetActive(false);
            Debug.Log("OnlinePanelManager - StartPhasePanel hidden");
        }
        else
        {
            Debug.LogError("OnlinePanelManager - startPhasePanel is null!");
        }
    }

    // 汎用的なフェーズパネル表示メソッド
    public void ShowPhasePanel(string title, string description)
    {
        ShowStartPhasePanel(title, description);
    }

    // 特定のフェーズ用のパネル表示メソッド
    public void ShowBettingPhasePanel()
    {
        Debug.Log("OnlinePanelManager - ShowBettingPhasePanel called");
        
        // スキルボタンを非表示
        VisibleSkillButtons(false);
        VisibleSkillSelectButtons(false);
        VisibleChangeCardButtons(false);
        
        // ベットボタンを表示
        VisibleBetButtons(true);
        // betAmountTextを表示
        if (playerLife != null) playerLife.gameObject.SetActive(false);
        if (betAmountText != null) betAmountText.gameObject.SetActive(true);
        
        // ベッティングパネルを表示（3秒後に自動非表示）
        ShowStartPhasePanel("Betting Phase", "ベット額を設定してください", 3f);
        
        Debug.Log("OnlinePanelManager - Betting Phase UI setup completed");
    }

    public void HideBettingPhasePanel()
    {
        Debug.Log("OnlinePanelManager - HideBettingPhasePanel called");
        
        if (startPhasePanel != null)
        {
            startPhasePanel.SetActive(false);
            Debug.Log("OnlinePanelManager - Betting Phase panel hidden");
        }
    }

    public void ShowRevealPhasePanel()
    {
        ShowStartPhasePanel("Reveal Phase", "カードを公開します");
    }

    public void ShowGameOverPanel(string result)
    {
        ShowStartPhasePanel("Game Over", result);
    }

    public void ShowSetCompletePanel()
    {
        if (setCompletePanel != null)
        {
            setCompletePanel.SetActive(true);
            Debug.Log("OnlinePanelManager - Set Complete panel activated");
            
            // 3秒後に自動的に非表示にする
            StartCoroutine(AutoHideSetCompletePanelAfterDelay(3f));
        }
        else
        {
            Debug.LogError("OnlinePanelManager - setCompletePanel is null in ShowSetCompletePanel()");
        }
    }

    // 相手アクション通知パネルを表示
    public void ShowOpponentActionPanel(string message, float duration = 3f)
    {
        if (opponentActionPanel != null && opponentActionText != null)
        {
            opponentActionText.text = message;
            opponentActionPanel.SetActive(true);
            Debug.Log($"OnlinePanelManager - Opponent action panel activated: {message}");
            
            // 指定時間後に自動的に非表示にする
            StartCoroutine(AutoHideOpponentActionPanelAfterDelay(duration));
        }
        else
        {
            Debug.LogError("OnlinePanelManager - opponentActionPanel or opponentActionText is null");
        }
    }

    private IEnumerator AutoHideOpponentActionPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (opponentActionPanel != null)
        {
            opponentActionPanel.SetActive(false);
            Debug.Log("OnlinePanelManager - Opponent action panel auto-hidden");
        }
    }

    private IEnumerator AutoHideSetCompletePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (setCompletePanel != null)
        {
            setCompletePanel.SetActive(false);
            Debug.Log("OnlinePanelManager - Set Complete panel auto-hidden");
        }
    }

    public void HideSetCompletePanel()
    {
        if (setCompletePanel != null)
        {
            setCompletePanel.SetActive(false);
            Debug.Log("OnlinePanelManager - Set Complete Panel hidden");
        }
    }

    // Open Phaseパネルを表示
    public void ShowOpenPhasePanel()
    {
        if (openPanel != null)
        {
            openPanel.SetActive(true);
            Debug.Log("OnlinePanelManager - Open Phase panel activated");
        }
        else
        {
            Debug.LogError("OnlinePanelManager - openPanel is null in ShowOpenPhasePanel()");
        }
    }

    // フェーズテキストを更新
    public void UpdatePhaseText(string phase)
    {
        if (phaseText != null)
        {
            string displayText = "";
            
            switch (phase.ToLower())
            {
                case "set_phase":
                    displayText = "SET PHASE";
                    break;
                case "betting":
                case "bet_phase":
                    displayText = "BET PHASE";
                    break;
                case "reveal":
                case "open_phase":
                    displayText = "OPEN PHASE";
                    break;
                default:
                    displayText = phase.ToUpper();
                    break;
            }
            
            phaseText.text = displayText;
            Debug.Log($"OnlinePanelManager - Phase text updated to: {displayText}");
        }
        else
        {
            Debug.LogWarning("OnlinePanelManager - phaseText is null! Please assign it in the Inspector.");
        }
    }
}


