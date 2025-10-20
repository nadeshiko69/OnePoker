using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OnlineDropZone : MonoBehaviour, IDropHandler
{
    private OnlineHandManager handManager;
    private OnlineGameManager gameManager;

    public bool isPlayerZone = true;
    public Image zoneImage; // Inspectorでアタッチ
    private Color defaultColor;

    void Start()
    {
        handManager = FindObjectOfType<OnlineHandManager>();
        gameManager = FindObjectOfType<OnlineGameManager>();

        if (zoneImage != null)
        {
            defaultColor = zoneImage.color;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("OnlineDropZone - OnDrop called");
        GameObject droppedObj = eventData.pointerDrag;
        
        if (isPlayerZone && droppedObj != null)
        {
            Debug.Log($"OnlineDropZone - Dropped object: {droppedObj.name}");
            
            // ドロップされたオブジェクトがプレイヤーのカードかチェック
            CardDisplay playerCard = droppedObj.GetComponent<CardDisplay>();
            if (playerCard != null && handManager != null)
            {
                // プレイヤーのカードかどうかチェック
                if (handManager.IsPlayerCard(playerCard))
                {
                    Debug.Log($"OnlineDropZone - Player card dropped: {playerCard.CardValue}");
                    
                    // 手札オブジェクトは動かさず、見た目用のクローンをSetZoneへ配置する
                    CardDraggable draggable = playerCard.GetComponent<CardDraggable>();
                    if (draggable != null)
                    {
                        // 元の位置・親に即時戻す（手札参照がMissingにならないようにする）
                        draggable.transform.SetParent(draggable.OriginalParent);
                        draggable.transform.position = draggable.OriginalPosition;
                    }

                    // クローンを生成してSetZone配下に配置
                    var clone = Instantiate(playerCard.gameObject, transform);
                    clone.name = "Card";
                    clone.transform.localPosition = Vector3.zero;
                    
                    // クローンはドラッグ不可にする
                    var cloneDraggable = clone.GetComponent<CardDraggable>();
                    if (cloneDraggable != null) Destroy(cloneDraggable);

                    // 値と表示を反映（表向き）
                    var cloneDisplay = clone.GetComponent<CardDisplay>();
                    if (cloneDisplay != null)
                    {
                        cloneDisplay.SetCardValue(playerCard.CardValue);
                        cloneDisplay.SetCard(true);
                    }

                    // ドロップ確認パネルを表示（ゲーム処理用にオリジナルの参照を渡す）
                    if (gameManager != null)
                    {
                        gameManager.ShowConfirmation(playerCard, this);
                    }
                    else
                    {
                        Debug.LogError("OnlineDropZone - gameManager is null!");
                    }
                }
                else
                {
                    Debug.Log("OnlineDropZone - Dropped card is not a player card");
                }
            }
            else
            {
                Debug.LogError("OnlineDropZone - playerCard or handManager is null!");
            }
        }
        else
        {
            Debug.Log($"OnlineDropZone - Drop condition not met: isPlayerZone={isPlayerZone}, droppedObj={droppedObj}");
        }
    }

    // デフォルト画像や色に戻す
    public void ResetZoneVisual()
    {
        Debug.Log("OnlineDropZone - ResetZoneVisual called");
        if (zoneImage != null)
        {
            zoneImage.color = defaultColor;
            zoneImage.sprite = null; // 必要ならスプライトもリセット
        }
        else 
        {
            Debug.Log("OnlineDropZone - zoneImage is null");
        }

        // DropZoneの子オブジェクト（カード）を全て削除
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
} 