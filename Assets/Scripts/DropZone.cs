using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    public bool isPlayerZone = true;
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (isPlayerZone)
        {
            if (droppedObj != null)
            {
                CardDraggable draggable = droppedObj.GetComponent<CardDraggable>();
                if (draggable != null)
                {
                    // ドラッグ対象のカードをこのDropZoneの子にする
                    draggable.transform.SetParent(transform);

                    // カードの位置を整える（中央揃え）
                    draggable.transform.localPosition = Vector3.zero;

                    // ドロップ確認パネルを表示
                    FindObjectOfType<GameManager>().ShowConfirmation(droppedObj, this);
                }
            }
        }
    }
}
