using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropZone : MonoBehaviour, IDropHandler
{
    public static DropZone[] allZones;

    private Image image;

    void Awake()
    {
        if (allZones == null)
            allZones = FindObjectsOfType<DropZone>();

        image = GetComponent<Image>();
        image.enabled = false;
    }

    public static void ShowDropZones(bool show)
    {
        foreach (var zone in allZones)
        {
            if (zone.image != null)
                zone.image.enabled = show;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (CardDraggable.draggedCard != null)
        {
            // 親だけ仮に移動（確定前）
            CardDraggable.draggedCard.transform.SetParent(transform);
            FindObjectOfType<PlacementManager>().ShowConfirmation(CardDraggable.draggedCard, this);
        }
    }
}
