using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropZone : MonoBehaviour, IDropHandler
{
    public bool isPlayerZone = true;
    public Image zoneImage; // Inspectorでアタッチ
    private Color defaultColor;

    void Start()
    {
        if (zoneImage != null)
        {
            defaultColor = zoneImage.color;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        GameObject playerCard = null;
        if (isPlayerZone)
        {
            if (droppedObj != null)
            {
                Debug.Log("droppedObj: " + droppedObj);
                if(droppedObj == GameObject.Find("Player_Card1"))
                {
                    Debug.Log(GameObject.Find("Player_Card1"));
                    Debug.Log("Value: " + GameObject.Find("Player_Card1").GetComponent<CardDisplay>().CardValue);
                    Debug.Log("Player_Card1");
                    playerCard = GameObject.Find("Player_Card1");
                }
                else if(droppedObj == GameObject.Find("Player_Card2"))
                {
                    Debug.Log("Value: " + GameObject.Find("Player_Card2").GetComponent<CardDisplay>().CardValue);
                    Debug.Log("Player_Card2");
                    playerCard = GameObject.Find("Player_Card2");
                }

                CardDraggable draggable = playerCard.GetComponent<CardDraggable>();
                if (draggable != null)
                {
                    // ドラッグ対象のカードをこのDropZoneの子にする
                    draggable.transform.SetParent(transform);

                    // カードの位置を整える（中央揃え）
                    draggable.transform.localPosition = Vector3.zero;

                    // ドロップ確認パネルを表示
                    FindObjectOfType<GameManager>().ShowConfirmation(playerCard, this);
                }
            }
        }
    }

    // デフォルト画像や色に戻す
    public void ResetZoneVisual()
    {
        Debug.Log("ResetZoneVisual called");
        if (zoneImage != null)
        {
            zoneImage.color = defaultColor;
            zoneImage.sprite = null; // 必要ならスプライトもリセット
        }
        else Debug.Log("zoneImage is null");

        // DropZoneの子オブジェクト（カード）を全て削除
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
