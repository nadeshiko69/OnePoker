using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

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

    void Start()
    {
        UpdateLifeUI();
        confirmationPanel.SetActive(false);
        bettingPanel.SetActive(false);
        randomChoiceCard = FindObjectOfType<RandomChoiceCard>();

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

        bet1Button.onClick.AddListener(() => PlaceBet(1));
        bet2Button.onClick.AddListener(() => PlaceBet(-1));
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
        ResetState();
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

        // リスナーをリセット
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
    }
}
