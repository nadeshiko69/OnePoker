using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class PlacementManager : MonoBehaviour
{
    public GameObject confirmationPanel;
    public Button yesButton;
    public Button noButton;

    private GameObject currentCard;
    private DropZone currentZone;
    private RandomChoiceCard randomChoiceCard;

    // For Debug ; 相手のカードを自動で配置
    private bool opponent_setCard = false;
    public GameObject opponentCard;
    public DropZone opponentZone;

    // プレイヤーのカードを保持
    private List<GameObject> playerCards = new List<GameObject>();

    // 両者カードを配置したらベット開始
    private bool bothCardsPlaced = false;

    // ベットフェーズ用のUI
    public GameObject bettingPanel;
    public TextMeshProUGUI playerLifeText;
    public TextMeshProUGUI opponentLifeText;
    private int MAX_LIFE = 20;
    private int MIN_LIFE = 0;
    private int playerLife = 20;
    private int opponentLife = 20;
    private int currentBetAmount = 0;
    public Button bet1Button;
    public Button bet2Button;
    
    public Button callButton;
    public TextMeshProUGUI callButtonText;

    private bool opponentCalled = false;
    private bool cardsRevealed = false;

    // オープンのUI
    public GameObject openPanel;
    private ResultViewManager resultViewManager;

    void Start()
    {
        UpdateLifeUI();
        confirmationPanel.SetActive(false);
        bettingPanel.SetActive(false);
        openPanel.SetActive(false);
        randomChoiceCard = FindObjectOfType<RandomChoiceCard>();
        resultViewManager = FindObjectOfType<ResultViewManager>();

        yesButton.onClick.AddListener(ConfirmPlacement);
        noButton.onClick.AddListener(CancelPlacement);

        // Callボタンの初期テキストを設定
        UpdateCallButtonText();
    }

    void Update()
    {
        if (opponent_setCard)
        {
            PlaceOpponentCard(opponentCard, opponentZone);
            opponent_setCard = false;
        }

        if (bothCardsPlaced)
        {
            // ベット開始
            Debug.Log("ベット開始");
            ShowBettingUI();
            bothCardsPlaced = false;
        }
    }

    private IEnumerator HideBettingUI()
    {
        yield return new WaitForSeconds(1f);
        bettingPanel.SetActive(false);
    }

    private IEnumerator SetOpponentCardFlag()
    {
        yield return new WaitForSeconds(1f);
        opponent_setCard = true;
    }
    
    private IEnumerator SetBettingPhase()
    {
        yield return new WaitForSeconds(1f);
        bothCardsPlaced = true;
    }

    private void UpdateLifeUI()
    {
        playerLifeText.text = "Life: " + playerLife;
        opponentLifeText.text = "Life: " + opponentLife;
    }

    private void UpdateCallButtonText()
    {
        if (callButtonText != null)
        {
            string action = currentBetAmount >= 2 ? "Raise" : "Call";
            callButtonText.text = $"{action} ({currentBetAmount})";
        }
    }

    private void ShowBettingUI()
    {
        bettingPanel.SetActive(true);
        StartCoroutine(HideBettingUI());

        // ベット開始時に自動で1をベット
        PlaceBet(1);

        bet1Button.onClick.RemoveAllListeners();
        bet2Button.onClick.RemoveAllListeners();
        callButton.onClick.RemoveAllListeners();

        bet1Button.onClick.AddListener(() => PlaceBet(1));
        bet2Button.onClick.AddListener(() => PlaceBet(-1));
        callButton.onClick.AddListener(() => StartCoroutine(HandleCall()));
    }

    private void PlaceBet(int amount)
    {
        if (amount > 0)
        {
            // ベット額を1増やす
            currentBetAmount += 1;

            if (playerLife >= 1) // 1ライフ以上あればベット可能
            {
                playerLife -= 1; // 1ずつライフを減らす
                Debug.Log($"Betting {currentBetAmount} life!");
                UpdateLifeUI();
                UpdateCallButtonText();
            }
            else
            {
                Debug.LogWarning("ライフが足りないためベットできません！");
                currentBetAmount -= 1; // ベット額を元に戻す
            }
        }
        else if (amount < 0)
        {
            // ベット額を1減らす
            if (currentBetAmount > 1) // 最小ベット額は1
            {
                currentBetAmount -= 1;
                playerLife += 1; // ライフを1戻す
                Debug.Log($"Reducing bet to {currentBetAmount} life!");
                UpdateLifeUI();
                UpdateCallButtonText();
            }
        }
    }

    private IEnumerator HandleCall()
    {
        // プレイヤーがコール
        Debug.Log($"Player calls with {currentBetAmount} life!");
        bettingPanel.SetActive(false);
        UpdateCallButtonText();

        yield return new WaitForSeconds(1f);

        // CPUがコール
        Debug.Log("Opponent calls!");
        opponentCalled = true;
        opponentLife -= currentBetAmount; // 相手のライフを減らす
        UpdateLifeUI();

        yield return new WaitForSeconds(1f);

        // オープンのUIを表示しカードオープン
        openPanel.SetActive(true);
        yield return new WaitForSeconds(1f);
        openPanel.SetActive(false);
        RevealCards();
    }

    private void RevealCards()
    {
        if (!cardsRevealed)
        {
            // プレイヤーのカードを表向きにする
            foreach (GameObject card in playerCards)
            {
                if (card != null)
                {
                    CardDisplay playerCardDisplay = card.GetComponent<CardDisplay>();
                    if (playerCardDisplay != null)
                    {
                        playerCardDisplay.SetCard(true);
                    }
                }
            }

            // 相手のカードを表向きにする
            if (opponentCard != null)
            {
                CardDisplay opponentCardDisplay = opponentCard.GetComponent<CardDisplay>();
                if (opponentCardDisplay != null)
                {
                    opponentCardDisplay.SetCard(true);
                }
            }

            cardsRevealed = true;

            // 勝敗判定を表示
            if (randomChoiceCard != null)
            {
                // resultViewManagerが見つからない場合は再取得を試みる
                if (resultViewManager == null)
                {
                    resultViewManager = FindObjectOfType<ResultViewManager>();
                    if (resultViewManager == null)
                    {
                        Debug.LogError("ResultViewManager not found in the scene!");
                        return;
                    }
                }
                StartCoroutine(ShowResultWithDelay(randomChoiceCard));
            }
            else
            {
                Debug.LogError("RandomChoiceCard not found!");
            }
        }
    }

    private IEnumerator ShowResultWithDelay(RandomChoiceCard randomChoiceCard)
    {
        if (resultViewManager == null || randomChoiceCard == null)
        {
            Debug.LogError("Required components are missing for showing results!");
            yield break;
        }

        // 1秒待ってから結果を表示
        yield return new WaitForSeconds(1f);

        // 最後に配置したカードの値を取得
        if (playerCards.Count > 0)
        {
            GameObject lastPlacedCard = playerCards[playerCards.Count - 1];
            CardDisplay cardDisplay = lastPlacedCard.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                int playerCardValue = cardDisplay.CardValue1;
                resultViewManager.ShowResult(playerCardValue, randomChoiceCard.OpponentCardValue1);
            }
            else
            {
                Debug.LogError("CardDisplay component not found on the last placed card!");
            }
        }
        else
        {
            Debug.LogError("No player cards found!");
        }

        // 3秒後に結果表示を消す
        yield return new WaitForSeconds(3f);
        resultViewManager.HideResult();
        
        // 結果表示後にカードリストをクリア
        ClearPlayerCards();
    }

    public void ShowConfirmation(GameObject card, DropZone zone)
    {
        Debug.Log("ShowConfirmation called");
        Debug.Log("card: " + card);
        Debug.Log("zone: " + zone);

        currentCard = card;
        currentZone = zone;
        confirmationPanel.SetActive(true);

        // リスナーの多重登録防止
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        yesButton.onClick.AddListener(ConfirmPlacement);
        noButton.onClick.AddListener(CancelPlacement);
    }

    private void ConfirmPlacement()
    {
        Debug.Log("ConfirmPlacement called");
        Debug.Log("currentCard: " + currentCard);
        Debug.Log("currentZone: " + currentZone);
        currentCard.transform.position = currentZone.transform.position;
        confirmationPanel.SetActive(false);

        // プレイヤーのカードをリストに追加
        playerCards.Add(currentCard);

        // カードの値を設定
        CardDisplay cardDisplay = currentCard.GetComponent<CardDisplay>();
        if (cardDisplay != null && randomChoiceCard != null)
        {
            cardDisplay.SetCardInfo(randomChoiceCard.PlayerCardValue);
        }

        // 状態をリセット（currentCardは保持）
        GameObject tempCard = currentCard;
        ResetState();
        currentCard = tempCard;

        StartCoroutine(SetOpponentCardFlag());
    }

    public void PlaceOpponentCard(GameObject card, DropZone zone)
    {
        Debug.Log("PlaceOpponentCard called");
        Debug.Log("currentCard: " + card);
        Debug.Log("currentZone: " + zone);

        if (card != null && zone != null)
        {
            // カードをゾーンの位置に移動
            card.transform.position = zone.transform.position;

            // カードの裏面を表示
            CardDisplay cardDisplay = card.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                cardDisplay.SetCard(false);
                
                // RandomChoiceCardからカード情報を取得して設定
                if (randomChoiceCard != null)
                {
                    // カードの情報を設定
                    cardDisplay.SetCardInfo(
                        randomChoiceCard.OpponentCardValue1
                    );
                }
            }
        }
        else
        {
            Debug.LogError("Card or zone is null!");
        }

        // 1秒待機後にベットフェーズに移行する
        StartCoroutine(SetBettingPhase());
    }

    private void CancelPlacement()
    {
        // カードを元の位置に戻す
        var drag = currentCard.GetComponent<CardDraggable>();
        if (drag != null)
        {
            currentCard.transform.SetParent(drag.OriginalParent);
            currentCard.transform.position = drag.OriginalPosition;
        }
        
        // パネル非表示 & 状態クリア
        confirmationPanel.SetActive(false);
        ResetState();
    }

    private void ResetState()
    {
        currentCard = null;
        currentZone = null;
        confirmationPanel.SetActive(false);
        opponentCalled = false;
        cardsRevealed = false;
        currentBetAmount = 0;
        UpdateCallButtonText();

        // リスナーをリセット
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
    }

    // ゲーム終了時にカードリストをクリア
    public void ClearPlayerCards()
    {
        playerCards.Clear();
    }
}
