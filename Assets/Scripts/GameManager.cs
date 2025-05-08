using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public GameObject confirmationPanel;
    public Button yesButton;
    public Button noButton;

    private GameObject currentCard;
    private DropZone currentZone;
    private DeckManager deckManager;

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

    // MatchManagerへの参照
    private MatchManager matchManager;

    void Start()
    {
        confirmationPanel.SetActive(false);
        bettingPanel.SetActive(false);
        openPanel.SetActive(false);
        deckManager = FindObjectOfType<DeckManager>();
        resultViewManager = FindObjectOfType<ResultViewManager>();
        matchManager = FindObjectOfType<MatchManager>();

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
        if (playerLifeText != null)
        {
            playerLifeText.text = $"Life: {matchManager.PlayerLife}";
        }
        if (opponentLifeText != null)
        {
            opponentLifeText.text = $"Life: {matchManager.OpponentLife}";
        }
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
            if (matchManager.PlayerLife >= 1) // 1ライフ以上あればベット可能
            {
                currentBetAmount += 1;
                matchManager.UpdatePlayerLife(-1); // 1ずつライフを減らす
                Debug.Log($"Betting {currentBetAmount} life!");
                UpdateLifeUI();
                UpdateCallButtonText();
            }
            else
            {
                Debug.LogWarning("ライフが足りないためベットできません！");
            }
        }
        else if (amount < 0)
        {
            // ベット額を1減らす
            if (currentBetAmount > 1) // 最小ベット額は1
            {
                currentBetAmount -= 1;
                matchManager.UpdatePlayerLife(1); // ライフを1戻す
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
        //　CPUのライフを更新する
        matchManager.UpdateOpponentLife(-currentBetAmount);

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
            Debug.Log("RevealCards called");
            Debug.Log("playerCards: " + playerCards);
            // プレイヤーのカードはすでに表向きなのでSetCard不要
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
            if (deckManager != null)
            {
                if (resultViewManager == null)
                {
                    resultViewManager = FindObjectOfType<ResultViewManager>();
                    if (resultViewManager == null)
                    {
                        Debug.LogError("ResultViewManager not found in the scene!");
                        return;
                    }
                }
                StartCoroutine(ShowResultWithDelay(deckManager));
            }
            else
            {
                Debug.LogError("deckManager not found!");
            }
        }
    }

    private IEnumerator ShowResultWithDelay(DeckManager deckManager)
    {
        if (resultViewManager == null || deckManager == null)
        {
            Debug.LogError("Required components are missing for showing results!");
            yield break;
        }

        yield return new WaitForSeconds(1f);

        if (playerCards.Count > 0)
        {
            GameObject lastPlacedCard = playerCards[playerCards.Count - 1];
            CardDisplay cardDisplay = lastPlacedCard.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                int playerCardValue = cardDisplay.CardValue1;
                int opponentCardValue = deckManager.OpponentCardValue1;
                Debug.Log($"Showing result - Player: {playerCardValue}, Opponent: {opponentCardValue}");
                resultViewManager.ShowResult(playerCardValue, opponentCardValue);
                
                // ライフの更新
                UpdateLife(playerCardValue, opponentCardValue);
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
        
        yield return new WaitForSeconds(3f);
        resultViewManager.HideResult();
        
        // // 結果表示後にカードリストをクリア
        // ClearPlayerCards();

        matchManager.OnGameComplete();
    }

    // ゲーム終了時にカードリストをクリア
    public void ClearPlayerCards()
    {
        playerCards.Clear();
    }

    // 1回の勝負が終わった後に残ライフを更新する
    private void UpdateLife(int playerValue, int opponentValue)
    {
        Debug.Log($"Before result - Player Life: {matchManager.PlayerLife}, Opponent Life: {matchManager.OpponentLife}, Bet: {currentBetAmount}");

        if (playerValue == opponentValue)
        {
            // 引き分けの場合は両者のライフを元に戻す
            matchManager.UpdatePlayerLife(currentBetAmount);
            matchManager.UpdateOpponentLife(currentBetAmount);
            Debug.Log("Draw - Returning bets");
        }
        else if (resultViewManager.IsWinner(playerValue, opponentValue))
        {
            // プレイヤーの勝利
            matchManager.UpdatePlayerLife(currentBetAmount*2);
            Debug.Log($"Player wins - Player gains opponent's bet: {currentBetAmount*2}");
        }
        else
        {
            // 相手の勝利
            matchManager.UpdateOpponentLife(currentBetAmount*2);
            Debug.Log($"CPU wins - CPU gains player's bet: {currentBetAmount*2}");
        }

        Debug.Log($"After result - Player Life: {matchManager.PlayerLife}, Opponent Life: {matchManager.OpponentLife}");
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
        currentCard.transform.position = currentZone.transform.position;
        confirmationPanel.SetActive(false);

        // プレイヤーのカードをリストに追加
        playerCards.Add(currentCard);

        // カードの値を設定
        CardDisplay cardDisplay = currentCard.GetComponent<CardDisplay>();
        if (cardDisplay != null && deckManager != null)
        {
            cardDisplay.SetCardInfo(cardDisplay.CardValue1);
        }

        // 状態をリセット（currentCardは保持）
        GameObject tempCard = currentCard;
        ResetState();
        currentCard = tempCard;

        StartCoroutine(SetOpponentCardFlag());
    }

    public void PlaceOpponentCard(GameObject card, DropZone zone)
    {
        if (card != null && zone != null)
        {
            // カードをDropZoneの子にする（親子関係をセット）
            card.transform.SetParent(zone.transform);

            // カードの位置を整える（中央揃え）
            card.transform.localPosition = Vector3.zero;

            // カードの裏面を表示
            CardDisplay cardDisplay = card.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                cardDisplay.SetCard(false);
                
                if (deckManager != null)
                {
                    cardDisplay.SetCardInfo(deckManager.OpponentCardValue1); // TODO ; 強制的に1枚目にしているので後ほど修正
                }
            }
        }
        else
        {
            Debug.LogError("Card or zone is null!");
        }

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
}
