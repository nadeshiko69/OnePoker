using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlacementManager : MonoBehaviour
{
    public GameObject confirmationPanel;
    public Button yesButton;
    public Button noButton;
    private bool opponent_setCard = false;

    private GameObject currentCard;
    private DropZone currentZone;
    private RandomChoiceCard randomChoiceCard;

    // For Debug ; 相手のカードを自動で配置
    public GameObject opponentCard;
    public DropZone opponentZone;

    void Start()
    {
        confirmationPanel.SetActive(false); // デフォルトでは非表示にしておく
        randomChoiceCard = FindObjectOfType<RandomChoiceCard>();

        yesButton.onClick.AddListener(ConfirmPlacement);
        noButton.onClick.AddListener(CancelPlacement);
    }

    void Update()
    {
        if (opponent_setCard)
        {
            PlaceOpponentCard(opponentCard, opponentZone);
            opponent_setCard = false;
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

        // 1秒待機後にopponent_setCardをtrueにする
        StartCoroutine(SetOpponentCardFlag());
    }

    private IEnumerator SetOpponentCardFlag()
    {
        yield return new WaitForSeconds(1f);
        opponent_setCard = true;
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
