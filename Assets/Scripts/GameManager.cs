using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    private GameObject currentCard;
    private DropZone currentZone;
    private DeckManager deckManager;
    private ResultViewManager resultViewManager;
    private PanelManager panelManager;

    // For Debug ; 相手のカードを自動で配置
    private bool opponent_setCard = false;
    public GameObject opponentCard;
    public DropZone opponentZone;

    // プレイヤーのカードを保持
    private GameObject SetPlayerCard;
    private GameObject SetOpponentCard;
    
    // 両者カードを配置したらベット開始
    private bool bothCardsPlaced = false;

    // ベットフェーズ用のUI
    public TextMeshProUGUI playerLifeText;
    public TextMeshProUGUI opponentLifeText;
    private int currentBetAmount = 0;
    public int CurrentBetAmount => currentBetAmount;

    public bool OpponentCalled = false;
    private bool cardsRevealed = false;

    // MatchManagerへの参照
    private MatchManager matchManager;

    void Start()
    {
        deckManager = FindObjectOfType<DeckManager>();
        resultViewManager = FindObjectOfType<ResultViewManager>();
        matchManager = FindObjectOfType<MatchManager>();
        panelManager = FindObjectOfType<PanelManager>();

        // Callボタンの初期テキストを設定
        panelManager.UpdateCallButtonText();
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
            panelManager.ShowBettingUI();
            bothCardsPlaced = false;
        }
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

    public void PlaceBet(int amount)
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
                panelManager.UpdateCallButtonText();
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
                panelManager.UpdateCallButtonText();
            }
        }
    }

    public void RevealCards()
    {
        if (!cardsRevealed)
        {
            // プレイヤーのカードを表向きにする
            Debug.Log("RevealCards called");
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

        if (SetPlayerCard != null)
        {
            int playerCardValue = SetPlayerCard.GetComponent<CardDisplay>().CardValue;
            int opponentCardValue = deckManager.OpponentCardValue1;
            Debug.Log($"Showing result - Player: {playerCardValue}, Opponent: {opponentCardValue}");
            panelManager.ShowResult(playerCardValue, opponentCardValue);
            resultViewManager.ShowResult(playerCardValue, opponentCardValue);
            UpdateLife(playerCardValue, opponentCardValue);
        }
        else
        {
            Debug.LogError("No player cards found!");
        }
        
        yield return new WaitForSeconds(3f);
        panelManager.HidePanel(panelManager.resultPanel);

        matchManager.OnGameComplete();
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
        Debug.Log("number: " + card.GetComponent<CardDisplay>().CardValue);
        currentCard = card;
        currentZone = zone;
        panelManager.confirmationPanel.SetActive(true);

        // リスナーの多重登録防止
        panelManager.yesButton.onClick.RemoveAllListeners();
        panelManager.noButton.onClick.RemoveAllListeners();

        panelManager.yesButton.onClick.AddListener(ConfirmPlacement);
        panelManager.noButton.onClick.AddListener(CancelPlacement);
    }

    public void ConfirmPlacement()
    {
        currentCard.transform.position = currentZone.transform.position;
        panelManager.confirmationPanel.SetActive(false);

        // プレイヤーのカードをリストに追加
        SetPlayerCard = currentCard;

        // カードの値を設定
        CardDisplay cardDisplay = SetPlayerCard.GetComponent<CardDisplay>();

        // 状態をリセット（currentCardは保持）
        GameObject tempCard = SetPlayerCard;
        ResetState();
        SetPlayerCard = tempCard;

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
            }
        }
        else
        {
            Debug.LogError("Card or zone is null!");
        }

        StartCoroutine(SetBettingPhase());
    }

    public void CancelPlacement()
    {
        // カードを元の位置に戻す
        var drag = currentCard.GetComponent<CardDraggable>();
        if (drag != null)
        {
            currentCard.transform.SetParent(drag.OriginalParent);
            currentCard.transform.position = drag.OriginalPosition;
        }
        
        // パネル非表示 & 状態クリア
        panelManager.confirmationPanel.SetActive(false);
        ResetState();
    }

    private void ResetState()
    {
        currentCard = null;
        currentZone = null;
        panelManager.confirmationPanel.SetActive(false);
        OpponentCalled = false;
        cardsRevealed = false;
        currentBetAmount = 0;
        panelManager.UpdateCallButtonText();

        // リスナーをリセット
        panelManager.yesButton.onClick.RemoveAllListeners();
        panelManager.noButton.onClick.RemoveAllListeners();
    }
}
