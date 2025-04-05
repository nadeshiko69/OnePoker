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
        currentCard.transform.position = currentZone.transform.position;
        confirmationPanel.SetActive(false);
    }

    private void CancelPlacement()
    {
        var drag = currentCard.GetComponent<CardDraggable>();
        currentCard.transform.SetParent(drag.OriginalParent);
        currentCard.transform.position = drag.OriginalPosition;
        confirmationPanel.SetActive(false);
    }
}
