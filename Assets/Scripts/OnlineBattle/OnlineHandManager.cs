using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class OnlineHandManager : MonoBehaviour
{
    public CardDisplay playerCard1;
    public CardDisplay playerCard2;
    public CardDisplay opponentCard1;
    public CardDisplay opponentCard2;
    
    // ========== デバッグ用フラグ ==========
    [Header("Debug Settings")]
    [SerializeField] private bool isDebugMode = true;

    // 参照がMissingの場合に自動で再取得する
    private void EnsureBindings()
    {
        Debug.Log($"[HAND_DEBUG] EnsureBindings() called");
        Debug.Log($"[HAND_DEBUG] Initial state: playerCard1={playerCard1 != null}, playerCard2={playerCard2 != null}, opponentCard1={opponentCard1 != null}, opponentCard2={opponentCard2 != null}");
        
        if (playerCard1 == null || !playerCard1.gameObject.activeInHierarchy) 
        {
            Debug.Log($"[HAND_DEBUG] playerCard1 needs recreation: null={playerCard1 == null}, active={playerCard1?.gameObject.activeInHierarchy}");
            playerCard1 = FindOrCreateCard("Player_Card1", "Player_CardAnchor1");
        }
        else
        {
            Debug.Log($"[HAND_DEBUG] playerCard1 exists, checking UI components: cardImage={playerCard1.cardImage != null}, numberText={playerCard1.numberText != null}, markText={playerCard1.markText != null}");
            if (playerCard1.cardImage == null || playerCard1.numberText == null || playerCard1.markText == null)
            {
                Debug.Log($"[HAND_DEBUG] playerCard1 has null UI components, setting references");
                SetUIComponentReferences(playerCard1, playerCard1.gameObject);
            }
        }
        
        if (playerCard2 == null || !playerCard2.gameObject.activeInHierarchy) 
        {
            Debug.Log($"[HAND_DEBUG] playerCard2 needs recreation: null={playerCard2 == null}, active={playerCard2?.gameObject.activeInHierarchy}");
            playerCard2 = FindOrCreateCard("Player_Card2", "Player_CardAnchor2");
        }
        else
        {
            Debug.Log($"[HAND_DEBUG] playerCard2 exists, checking UI components: cardImage={playerCard2.cardImage != null}, numberText={playerCard2.numberText != null}, markText={playerCard2.markText != null}");
            if (playerCard2.cardImage == null || playerCard2.numberText == null || playerCard2.markText == null)
            {
                Debug.Log($"[HAND_DEBUG] playerCard2 has null UI components, setting references");
                SetUIComponentReferences(playerCard2, playerCard2.gameObject);
            }
        }
        
        if (opponentCard1 == null || !opponentCard1.gameObject.activeInHierarchy) 
        {
            Debug.Log($"[HAND_DEBUG] opponentCard1 needs recreation: null={opponentCard1 == null}, active={opponentCard1?.gameObject.activeInHierarchy}");
            opponentCard1 = FindOrCreateCard("Opponent_Card1", "Opponent_CardAnchor1");
        }
        else
        {
            Debug.Log($"[HAND_DEBUG] opponentCard1 exists, checking UI components: cardImage={opponentCard1.cardImage != null}, numberText={opponentCard1.numberText != null}, markText={opponentCard1.markText != null}");
            if (opponentCard1.cardImage == null || opponentCard1.numberText == null || opponentCard1.markText == null)
            {
                Debug.Log($"[HAND_DEBUG] opponentCard1 has null UI components, setting references");
                SetUIComponentReferences(opponentCard1, opponentCard1.gameObject);
            }
        }
        
        if (opponentCard2 == null || !opponentCard2.gameObject.activeInHierarchy) 
        {
            Debug.Log($"[HAND_DEBUG] opponentCard2 needs recreation: null={opponentCard2 == null}, active={opponentCard2?.gameObject.activeInHierarchy}");
            opponentCard2 = FindOrCreateCard("Opponent_Card2", "Opponent_CardAnchor2");
        }
        else
        {
            Debug.Log($"[HAND_DEBUG] opponentCard2 exists, checking UI components: cardImage={opponentCard2.cardImage != null}, numberText={opponentCard2.numberText != null}, markText={opponentCard2.markText != null}");
            if (opponentCard2.cardImage == null || opponentCard2.numberText == null || opponentCard2.markText == null)
            {
                Debug.Log($"[HAND_DEBUG] opponentCard2 has null UI components, setting references");
                SetUIComponentReferences(opponentCard2, opponentCard2.gameObject);
            }
        }
    }

    // 指定アンカー配下にカードPrefabを再生成して返す（まず既存を探索）
    private CardDisplay FindOrCreateCard(string cardName, string anchorName)
    {
        var existing = GameObject.Find(cardName);
        if (existing != null)
        {
            var cd = existing.GetComponent<CardDisplay>();
            if (cd != null && existing.activeInHierarchy)
            {
                Debug.Log($"[HAND_DEBUG] Found existing active card: {cardName}");
                
                // 既存のカードでもUIコンポーネントがnullの場合は再設定
                if (cd.cardImage == null || cd.numberText == null || cd.markText == null)
                {
                    Debug.Log($"[HAND_DEBUG] Existing card has null UI components, setting references for {cardName}");
                    SetUIComponentReferences(cd, existing);
                }
                
                return cd;
            }
            else if (cd != null && !existing.activeInHierarchy)
            {
                Debug.Log($"[HAND_DEBUG] Found existing inactive card: {cardName}, destroying and recreating");
                Destroy(existing);
            }
        }

        var anchor = GameObject.Find(anchorName);
        if (anchor == null)
        {
            Debug.LogWarning($"[HAND_DEBUG] Anchor not found: {anchorName}");
            return null;
        }
        Debug.Log($"[HAND_DEBUG] Found anchor: {anchorName}, transform: {anchor.transform}");

        // PrefabをResourcesからロードして生成
        Debug.Log($"[HAND_DEBUG] Loading prefab from Resources/Prefabs/Card");
        var prefab = Resources.Load<GameObject>("Prefabs/Card");
        if (prefab == null)
        {
            Debug.LogError("[HAND_DEBUG] Card prefab not found at Resources/Prefabs/Card");
            return null;
        }
        Debug.Log($"[HAND_DEBUG] Prefab loaded successfully: {prefab.name}");

        var go = Instantiate(prefab, anchor.transform);
        go.name = cardName;
        Debug.Log($"[HAND_DEBUG] Instantiated card: {cardName} under {anchorName}");
        
        // アンカー中央に整列
        var rt = go.transform as RectTransform;
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            Debug.Log($"[HAND_DEBUG] Set card position: anchoredPosition={rt.anchoredPosition}, localScale={rt.localScale}");
        }
        
        // 既存のカードと同じRotationとScaleを設定
        var existingCard = GameObject.Find(cardName == "Player_Card1" ? "Player_Card2" : "Player_Card1");
        if (existingCard != null)
        {
            var existingRectTransform = existingCard.GetComponent<RectTransform>();
            if (existingRectTransform != null && rt != null)
            {
                rt.localRotation = existingRectTransform.localRotation;
                rt.localScale = existingRectTransform.localScale;
            }
        }

        var display = go.GetComponent<CardDisplay>();
        if (display == null)
        {
            display = go.AddComponent<CardDisplay>();
        }
        
        // UIコンポーネントの参照を設定（新しく生成されたPrefabの場合）
        if (display.cardImage == null || display.numberText == null || display.markText == null)
        {
            Debug.Log($"[HAND_DEBUG] Setting UI component references for {cardName}");
            SetUIComponentReferences(display, go);
        }
        
        // ドラッグ可能にしたい場合はCardDraggableが付いている前提（無ければ付与可能）
        if (go.GetComponent<CardDraggable>() == null)
        {
            go.AddComponent<CardDraggable>();
        }

        Debug.Log($"[HAND_DEBUG] Recreated missing card under {anchorName}: {cardName}");
        return display;
    }

    // UIコンポーネントの参照を設定する共通メソッド
    private void SetUIComponentReferences(CardDisplay display, GameObject cardObject)
    {
        Debug.Log($"[HAND_DEBUG] Attempting to set UI component references for {cardObject.name}");
        
        // Imageコンポーネントを取得
        if (display.cardImage == null)
        {
            display.cardImage = cardObject.GetComponent<Image>();
            if (display.cardImage == null)
            {
                display.cardImage = cardObject.GetComponentInChildren<Image>();
                if (display.cardImage != null)
                {
                    Debug.Log($"[HAND_DEBUG] Found Image in children for {cardObject.name}");
                }
                else
                {
                    Debug.LogError($"[HAND_DEBUG] Failed to find Image component for {cardObject.name}");
                }
            }
            else
            {
                Debug.Log($"[HAND_DEBUG] Found Image on root for {cardObject.name}");
            }
        }
        else
        {
            Debug.Log($"[HAND_DEBUG] Image already set for {cardObject.name}");
        }
        
        // TextMeshProUGUIコンポーネントを取得
        var textComponents = cardObject.GetComponentsInChildren<TextMeshProUGUI>();
        Debug.Log($"[HAND_DEBUG] Found {textComponents.Length} TextMeshProUGUI components in children for {cardObject.name}");
        
        if (textComponents.Length >= 2)
        {
            display.numberText = textComponents[0];
            display.markText = textComponents[1];
            Debug.Log($"[HAND_DEBUG] Assigned numberText and markText for {cardObject.name}");
        }
        else if (textComponents.Length == 1)
        {
            display.numberText = textComponents[0];
            display.markText = textComponents[0]; // 同じコンポーネントを使用
            Debug.Log($"[HAND_DEBUG] Assigned single TextMeshProUGUI to both numberText and markText for {cardObject.name}");
        }
        else
        {
            Debug.LogWarning($"[HAND_DEBUG] No TextMeshProUGUI components found for {cardObject.name}");
        }
        
        Debug.Log($"[HAND_DEBUG] UI components set: cardImage={display.cardImage != null}, numberText={display.numberText != null}, markText={display.markText != null}");
    }

    // プレイヤーの手札をUIに反映
    public void SetPlayerHand(int[] cardIds)
    {
        Debug.Log($"[HAND_DEBUG] SetPlayerHand called with cardIds: {(cardIds != null ? string.Join(",", cardIds) : "null")}");
        Debug.Log($"[HAND_DEBUG] About to call EnsureBindings()");
        EnsureBindings();
        Debug.Log($"[HAND_DEBUG] EnsureBindings() completed");
        Debug.Log($"[HAND_DEBUG] playerCard1: {playerCard1 != null}, playerCard2: {playerCard2 != null}");
        
        try
        {
        
        if (playerCard1 != null)
        {
            Debug.Log($"[HAND_DEBUG] playerCard1 GameObject active: {playerCard1.gameObject.activeInHierarchy}, enabled: {playerCard1.enabled}");
            
            // コンポーネントが無効になっている場合は有効にする
            if (!playerCard1.enabled)
            {
                Debug.Log("[HAND_DEBUG] playerCard1 component was disabled, enabling it");
                playerCard1.enabled = true;
            }
            
            if (cardIds != null && cardIds.Length > 0)
            {
                Debug.Log($"Setting playerCard1 to cardId: {cardIds[0]}");
                playerCard1.SetCardValue(cardIds[0]);
                playerCard1.SetCard(true); // 表向き
                Debug.Log($"[HAND_DEBUG] playerCard1.SetCard completed");
            }
            else
            {
                Debug.Log("Setting playerCard1 to default cardId: 0");
                playerCard1.SetCardValue(0);
                playerCard1.SetCard(true);
            }
        }
        else
        {
            Debug.LogError("playerCard1 is null!");
        }
        
        if (playerCard2 != null)
        {
            Debug.Log($"[HAND_DEBUG] playerCard2 GameObject active: {playerCard2.gameObject.activeInHierarchy}, enabled: {playerCard2.enabled}");
            
            // コンポーネントが無効になっている場合は有効にする
            if (!playerCard2.enabled)
            {
                Debug.Log("[HAND_DEBUG] playerCard2 component was disabled, enabling it");
                playerCard2.enabled = true;
            }
            
            if (cardIds != null && cardIds.Length > 1)
            {
                Debug.Log($"Setting playerCard2 to cardId: {cardIds[1]}");
                playerCard2.SetCardValue(cardIds[1]);
                playerCard2.SetCard(true); // 表向き
                Debug.Log($"[HAND_DEBUG] playerCard2.SetCard completed");
            }
            else
            {
                Debug.Log("Setting playerCard2 to default cardId: 0");
                playerCard2.SetCardValue(0);
                playerCard2.SetCard(true);
            }
        }
        else
        {
            Debug.LogError("playerCard2 is null!");
        }
        
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HAND_DEBUG] Exception in SetPlayerHand: {e.Message}");
        }
    }

    // 相手の手札をUIに反映（初期は裏向き）
    public void SetOpponentHand(int[] cardIds)
    {
        Debug.Log($"[HAND_DEBUG] SetOpponentHand called with cardIds: {(cardIds != null ? string.Join(",", cardIds) : "null")}");
        Debug.Log($"[HAND_DEBUG] About to call EnsureBindings()");
        EnsureBindings();
        Debug.Log($"[HAND_DEBUG] EnsureBindings() completed");
        Debug.Log($"[HAND_DEBUG] opponentCard1: {opponentCard1 != null}, opponentCard2: {opponentCard2 != null}");
        
        try
        {
        
        if (opponentCard1 != null)
        {
            Debug.Log($"[HAND_DEBUG] opponentCard1 GameObject active: {opponentCard1.gameObject.activeInHierarchy}, enabled: {opponentCard1.enabled}");
            
            // コンポーネントが無効になっている場合は有効にする
            if (!opponentCard1.enabled)
            {
                Debug.Log("[HAND_DEBUG] opponentCard1 component was disabled, enabling it");
                opponentCard1.enabled = true;
            }
            
            if (cardIds != null && cardIds.Length > 0)
            {
                Debug.Log($"Setting opponentCard1 to cardId: {cardIds[0]}");
                opponentCard1.SetCardValue(cardIds[0]);
                opponentCard1.SetCard(isDebugMode); // Debugなら表向きで表示
                Debug.Log($"[HAND_DEBUG] opponentCard1.SetCard completed - isDebugMode: {isDebugMode}");
            }
            else
            {
                Debug.Log("Setting opponentCard1 to default cardId: 0");
                opponentCard1.SetCardValue(0);
                opponentCard1.SetCard(isDebugMode); // Debugなら表向きで表示
            }
        }
        else
        {
            Debug.LogError("opponentCard1 is null!");
        }
        
        if (opponentCard2 != null)
        {
            Debug.Log($"[HAND_DEBUG] opponentCard2 GameObject active: {opponentCard2.gameObject.activeInHierarchy}, enabled: {opponentCard2.enabled}");
            
            // コンポーネントが無効になっている場合は有効にする
            if (!opponentCard2.enabled)
            {
                Debug.Log("[HAND_DEBUG] opponentCard2 component was disabled, enabling it");
                opponentCard2.enabled = true;
            }
            
            if (cardIds != null && cardIds.Length > 1)
            {
                Debug.Log($"Setting opponentCard2 to cardId: {cardIds[1]}");
                opponentCard2.SetCardValue(cardIds[1]);
                opponentCard2.SetCard(isDebugMode); // Debugなら表向きで表示
                Debug.Log($"[HAND_DEBUG] opponentCard2.SetCard completed - isDebugMode: {isDebugMode}");
            }
            else
            {
                Debug.Log("Setting opponentCard2 to default cardId: 0");
                opponentCard2.SetCardValue(0);
                opponentCard2.SetCard(isDebugMode); // Debugなら表向きで表示
            }
        }
        else
        {
            Debug.LogError("opponentCard2 is null!");
        }
        
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HAND_DEBUG] Exception in SetOpponentHand: {e.Message}");
        }
    }

    // 指定されたカードがプレイヤーのカードかどうかを判定
    public bool IsPlayerCard(CardDisplay card)
    {
        if (card == null) return false;
        
        return card == playerCard1 || card == playerCard2;
    }
    
    // ========== Scanスキル用の手札選択機能 ==========
    
    private OnlineSkillManager skillManager;
    private bool isOpponentCardSelectable = false;
    
    /// <summary>
    /// 相手の手札をクリック可能にする（Scanスキル用）
    /// </summary>
    public void EnableOpponentCardSelection(OnlineSkillManager manager)
    {
        Debug.Log("[HAND_MANAGER] Enabling opponent card selection for Scan skill");
        skillManager = manager;
        isOpponentCardSelectable = true;
        
        // 相手の手札にRaycastを有効にする
        if (opponentCard1 != null && opponentCard1.gameObject != null)
        {
            opponentCard1.gameObject.layer = 5; // UIレイヤー
            var raycaster = opponentCard1.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (raycaster == null)
            {
                raycaster = opponentCard1.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            
            // クリックイベントを追加
            UnityEngine.EventSystems.EventTrigger.Entry entry = new UnityEngine.EventSystems.EventTrigger.Entry();
            entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => OnOpponentCardClicked(opponentCard1.gameObject));
            raycaster.triggers.Add(entry);
        }
        
        if (opponentCard2 != null && opponentCard2.gameObject != null)
        {
            opponentCard2.gameObject.layer = 5; // UIレイヤー
            var raycaster = opponentCard2.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (raycaster == null)
            {
                raycaster = opponentCard2.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            
            // クリックイベントを追加
            UnityEngine.EventSystems.EventTrigger.Entry entry = new UnityEngine.EventSystems.EventTrigger.Entry();
            entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => OnOpponentCardClicked(opponentCard2.gameObject));
            raycaster.triggers.Add(entry);
        }
    }
    
    /// <summary>
    /// 相手の手札のクリック可能状態を解除
    /// </summary>
    public void DisableOpponentCardSelection()
    {
        Debug.Log("[HAND_MANAGER] Disabling opponent card selection");
        isOpponentCardSelectable = false;
        
        // イベントトリガーを削除
        if (opponentCard1 != null && opponentCard1.gameObject != null)
        {
            var raycaster = opponentCard1.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (raycaster != null)
            {
                raycaster.triggers.Clear();
            }
        }
        
        if (opponentCard2 != null && opponentCard2.gameObject != null)
        {
            var raycaster = opponentCard2.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (raycaster != null)
            {
                raycaster.triggers.Clear();
            }
        }
    }
    
    /// <summary>
    /// 相手の手札がクリックされた時の処理
    /// </summary>
    private void OnOpponentCardClicked(GameObject card)
    {
        if (!isOpponentCardSelectable || skillManager == null)
        {
            return;
        }
        
        Debug.Log($"[HAND_MANAGER] Opponent card clicked: {card.name}");
        skillManager.OnOpponentCardSelected(card);
    }
    
    /// <summary>
    /// Scanスキルで表向きになった相手の手札を裏向きに戻す
    /// Debugなら表向きのまま
    /// </summary>
    public void ResetOpponentCardsToFaceDown()
    {
        Debug.Log($"[HAND_MANAGER] Resetting opponent cards to face down (isDebugMode: {isDebugMode})");
        
        if (opponentCard1 != null)
        {
            opponentCard1.SetCard(isDebugMode); // Debugなら表向きで表示
            Debug.Log($"[HAND_MANAGER] opponentCard1 set to {(isDebugMode ? "face up" : "face down")}");
        }
        
        if (opponentCard2 != null)
        {
            opponentCard2.SetCard(isDebugMode); // Debugなら表向きで表示
            Debug.Log($"[HAND_MANAGER] opponentCard2 set to {(isDebugMode ? "face up" : "face down")}");
        }
    }
} 