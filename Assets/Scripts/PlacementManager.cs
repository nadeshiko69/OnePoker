using UnityEngine;
using UnityEngine.UI;

public class PlacementManager : MonoBehaviour
{
    public GameObject confirmationPanel;
    public Button yesButton;
    public Button noButton;

    private GameObject currentCard;
    private DropZone currentZone;

    void Start()
    {
        confirmationPanel.SetActive(false); // デフォルトでは非表示にしておく

        yesButton.onClick.AddListener(ConfirmPlacement);
        noButton.onClick.AddListener(CancelPlacement);
    }

    public void ShowConfirmation(GameObject card, DropZone zone)
    {
        currentCard = card;
        currentZone = zone;
        confirmationPanel.SetActive(true);
    }

    private void ConfirmPlacement()
    {
        // カードをドロップゾーンに正式に配置
        currentCard.transform.SetParent(currentZone.transform);
        currentCard.transform.localPosition = Vector3.zero;

        // パネル非表示 & 状態クリア
        confirmationPanel.SetActive(false);
        ResetState();
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
    }
}
