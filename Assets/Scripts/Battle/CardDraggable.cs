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
        // CanvasGroupが存在しない場合は追加
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // オンライン対戦時のカードセット可能フラグをチェック（一時的に無効化）
        /*
        if (IsOnlineBattle() && !CanSetCardInOnlineBattle())
        {
            Debug.Log("CardDraggable - Cannot drag card in online battle: Set Phase not active");
            Debug.Log($"CardDraggable - Debug: IsOnlineBattle={IsOnlineBattle()}, CanSetCardInOnlineBattle={CanSetCardInOnlineBattle()}");
            return;
        }
        */
        Debug.Log("CardDraggable - Drag check bypassed for testing");

        draggedCard = gameObject;
        originalPosition = transform.position;
        originalParent = transform.parent;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f; // 透明度を下げる
            canvasGroup.blocksRaycasts = false; // 他のオブジェクトがドロップを受けられるようにする
        }

        // 親をCanvasに変更（ドラッグ時に他の UI の影響を受けないように）
        transform.SetParent(transform.root);
        
        Debug.Log("CardDraggable - Started dragging card");
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // 何もドロップしなかったら元の位置に戻す
        if (transform.parent == transform.root)
        {
            transform.SetParent(originalParent);
            transform.position = originalPosition;
        }

        draggedCard = null;
        Debug.Log("CardDraggable - Ended dragging card");
    }

    // オンライン対戦かどうかを判定
    private bool IsOnlineBattle()
    {
        // OnlineGameManagerが存在するかどうかで判定
        return FindObjectOfType<OnlineGameManager>() != null;
    }

    // オンライン対戦時にカードセット可能かどうかを判定
    private bool CanSetCardInOnlineBattle()
    {
        var onlineGameManager = FindObjectOfType<OnlineGameManager>();
        if (onlineGameManager != null)
        {
            bool canSet = onlineGameManager.CanSetCard();
            Debug.Log($"CardDraggable - CanSetCardInOnlineBattle: {canSet}");
            return canSet;
        }
        Debug.Log("CardDraggable - CanSetCardInOnlineBattle: No OnlineGameManager found, returning true");
        return true; // オンライン対戦でない場合は常にtrue
    }
}
