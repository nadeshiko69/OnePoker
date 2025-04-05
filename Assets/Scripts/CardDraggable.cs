using UnityEngine;
using UnityEngine.EventSystems;

public class CardDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;

    public static GameObject draggedCard; // 現在ドラッグ中のカード

    public Transform OriginalParent => originalParent;
    public Vector3 OriginalPosition => originalPosition;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        draggedCard = gameObject;
        originalPosition = transform.position;
        originalParent = transform.parent;

        canvasGroup.alpha = 0.6f; // 透明度を下げる
        canvasGroup.blocksRaycasts = false; // 他のオブジェクトがドロップを受けられるようにする

        // 親をCanvasに変更（ドラッグ時に他の UI の影響を受けないように）
        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 何もドロップしなかったら元の位置に戻す
        if (transform.parent == transform.root)
        {
            transform.SetParent(originalParent);
            transform.position = originalPosition;
        }

        draggedCard = null;
    }
}
