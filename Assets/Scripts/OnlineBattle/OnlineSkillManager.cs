using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// オンライン対戦用スキル管理
/// - SetPhaseでのみスキルを使用可能
/// - 使用したスキルはサーバーに送信され、DynamoDBに保存
/// - 相手が使用したスキルはAWSから読み込んで表示（次のターンで表示）
/// </summary>
public class OnlineSkillManager : MonoBehaviour
{
    [Header("依存マネージャー")]
    private OnlineGameDataProvider gameDataProvider;
    private OnlinePanelManager panelManager;
    private OnlinePhaseManager phaseManager;

    [Header("Obstruct状態管理")]
    private bool isPlayerObstructed = false;
    public bool IsPlayerObstructed => isPlayerObstructed;
    
    // Obstruct効果を受けたターン数を記録
    private int obstructTurnCounter = 0;
    
    // 既に処理済みのObstructかどうかを記録
    private bool obstructAlreadyProcessed = false;

    [Header("使用済スキル")]
    private List<string> myUsedSkills = new List<string>();
    private List<string> opponentUsedSkills = new List<string>();
    
    // 前回のSetPhaseで通知済みの相手のスキル（重複通知を防ぐため）
    private List<string> lastNotifiedOpponentSkills = new List<string>();

    void Start()
    {
        gameDataProvider = FindObjectOfType<OnlineGameDataProvider>();
        panelManager = FindObjectOfType<OnlinePanelManager>();
        phaseManager = FindObjectOfType<OnlinePhaseManager>();

        Debug.Log($"[SkillManager] Start() - GameDataProvider found: {gameDataProvider != null}");
        Debug.Log($"[SkillManager] Start() - PanelManager found: {panelManager != null}");
        Debug.Log($"[SkillManager] Start() - PhaseManager found: {phaseManager != null}");
        
        if (gameDataProvider != null)
        {
            Debug.Log($"[SkillManager] Start() - GameId: '{gameDataProvider.GameId}'");
            Debug.Log($"[SkillManager] Start() - MyPlayerId: '{gameDataProvider.MyPlayerId}'");
            Debug.Log($"[SkillManager] Start() - GameData loaded: {gameDataProvider.GameData != null}");
        }
        
        if (gameDataProvider == null) Debug.LogError("OnlineSkillManager - OnlineGameDataProvider not found!");
        if (panelManager == null) Debug.LogError("OnlineSkillManager - OnlinePanelManager not found!");
    }

    /// <summary>
    /// SetPhase開始時に呼び出し、スキルUIを表示
    /// </summary>
    public void OnSetPhaseStarted()
    {
        Debug.Log("[SkillManager] SetPhase started - Enabling skill UI");
        
        // ローカルのキャッシュから即時更新（表示ラグ低減）
        UpdateUsedSkillsFromServer();

        // 最新のGameStateを取得して使用済みスキルを同期
        if (gameDataProvider != null)
        {
            OnePoker.Network.HttpManager.Instance.GetGameState(
                gameDataProvider.GameId,
                gameDataProvider.PlayerId,
                OnSetPhaseGameStateReceived,
                (error) => { Debug.LogError($"[SkillManager] Failed to refresh used skills on SetPhase: {error}"); }
            );
        }
        
        // Obstruct状態をチェック（1ターンのみ有効）
        CheckAndUpdateObstructStatus();

        // スキルUIを表示
        if (panelManager != null)
        {
            panelManager.ShowSkillUI();
            UpdateSkillButtonStates();
            // 使用済スキル表示を更新
            panelManager.UpdateUsedSkillsDisplay(myUsedSkills, opponentUsedSkills);
        }
    }

    // SetPhase開始時に取得したGameStateのハンドラ
    private void OnSetPhaseGameStateReceived(string response)
    {
        try
        {
            var gameState = JsonUtility.FromJson<SetPhaseGameState>(response);
            if (gameState != null)
            {
                var p1 = gameState.player1UsedSkills != null ? new List<string>(gameState.player1UsedSkills) : new List<string>();
                var p2 = gameState.player2UsedSkills != null ? new List<string>(gameState.player2UsedSkills) : new List<string>();

                // Providerへ反映
                gameDataProvider.UpdateUsedSkills(p1, p2);

                // ローカルキャッシュとUIを更新
                UpdateUsedSkillsFromServer();
                UpdateSkillButtonStates();
                if (panelManager != null)
                {
                    panelManager.UpdateUsedSkillsDisplay(myUsedSkills, opponentUsedSkills);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SkillManager] Error parsing game state on SetPhase: {e.Message}");
        }
    }

    [System.Serializable]
    private class SetPhaseGameState
    {
        public string gameId;
        public string gamePhase;
        public string[] player1UsedSkills;
        public string[] player2UsedSkills;
    }

    /// <summary>
    /// BettingPhase/RevealPhase開始時に呼び出し、スキルUIを非表示
    /// </summary>
    public void OnNonSetPhaseStarted()
    {
        Debug.Log("[SkillManager] Non-SetPhase started - Hiding skill UI");
        if (panelManager != null)
        {
            panelManager.HideSkillUI();
        }
    }

    /// <summary>
    /// サーバーから使用済スキルを更新
    /// </summary>
    public void UpdateUsedSkillsFromServer()
    {
        if (gameDataProvider == null) return;

        var gameData = gameDataProvider.GameData;
        if (gameData == null) return;

        // 自分と相手の使用済スキルを更新
        bool isPlayer1 = gameDataProvider.IsPlayer1;
        
        myUsedSkills = isPlayer1 
            ? (gameData.player1UsedSkills ?? new List<string>()) 
            : (gameData.player2UsedSkills ?? new List<string>());
        
        opponentUsedSkills = isPlayer1 
            ? (gameData.player2UsedSkills ?? new List<string>()) 
            : (gameData.player1UsedSkills ?? new List<string>());

        Debug.Log($"[SkillManager] My used skills: {string.Join(", ", myUsedSkills)}");
        Debug.Log($"[SkillManager] Opponent used skills: {string.Join(", ", opponentUsedSkills)}");

        // 相手のスキル表示を更新
        UpdateOpponentSkillDisplay();

        // 使用済スキル表示（ラベル）も更新
        if (panelManager != null)
        {
            panelManager.UpdateUsedSkillsDisplay(myUsedSkills, opponentUsedSkills);
        }
    }

    /// <summary>
    /// 相手が使用したスキルをUI上に表示
    /// 注意: SetPhaseの時だけ通知を表示（BetPhaseなど他のフェーズでは表示しない）
    /// また、前回のSetPhaseで既に通知したスキルは再度通知しない（重複通知を防ぐ）
    /// </summary>
    private void UpdateOpponentSkillDisplay()
    {
        if (panelManager == null) return;

        // SetPhaseの時だけ通知を表示（BetPhaseやRevealPhaseでは表示しない）
        bool isSetPhase = phaseManager != null && phaseManager.IsSetPhaseActive;
        if (!isSetPhase)
        {
            Debug.Log($"[SkillManager] UpdateOpponentSkillDisplay called but not in SetPhase - skipping notification");
            return;
        }

        // 前回のSetPhase以降に追加された新しいスキルを特定
        List<string> newSkills = new List<string>();
        foreach (string skillType in opponentUsedSkills)
        {
            if (!lastNotifiedOpponentSkills.Contains(skillType))
            {
                newSkills.Add(skillType);
            }
        }

        // 新しいスキルのみ通知を表示
        foreach (string skillType in newSkills)
        {
            Debug.Log($"[SkillManager] New opponent skill detected: {skillType}");
            
            // Scanスキルの場合は通知を表示
            if (skillType == "Scan")
            {
                panelManager.ShowOpponentUsedSkillNotification("相手がScanを使用しました");
            }
            else if (skillType == "FakeOut")
            {
                panelManager.ShowOpponentUsedSkillNotification("相手がScanを使用しました"); // ブラフ
            }
        }

        // 通知したスキルを記録（次回のSetPhaseで重複通知を防ぐ）
        if (newSkills.Count > 0)
        {
            lastNotifiedOpponentSkills = new List<string>(opponentUsedSkills);
            Debug.Log($"[SkillManager] Updated lastNotifiedOpponentSkills: {string.Join(", ", lastNotifiedOpponentSkills)}");
        }
    }

    /// <summary>
    /// スキルボタンの有効/無効状態を更新
    /// </summary>
    private void UpdateSkillButtonStates()
    {
        if (panelManager == null) return;

        // Obstructされている場合は全スキル無効
        if (isPlayerObstructed)
        {
            Debug.Log("[SkillManager] Player is obstructed - disabling all skills");
            panelManager.SetAllSkillButtonsInteractable(false);
            return;
        }

        // 各スキルの使用可能状態を更新
        panelManager.SetSkillButtonInteractable("Scan", !myUsedSkills.Contains("Scan"));
        panelManager.SetSkillButtonInteractable("Change", !myUsedSkills.Contains("Change"));
        panelManager.SetSkillButtonInteractable("Obstruct", !myUsedSkills.Contains("Obstruct"));
        panelManager.SetSkillButtonInteractable("FakeOut", !myUsedSkills.Contains("FakeOut"));
        panelManager.SetSkillButtonInteractable("Copy", !myUsedSkills.Contains("Copy"));
    }

    /// <summary>
    /// スキル使用（サーバーに送信）
    /// </summary>
    public void UseSkill(string skillType)
    {
        Debug.Log($"[SkillManager] UseSkill called: {skillType}");
        Debug.Log($"[SkillManager] UseSkill - GameDataProvider: {gameDataProvider != null}");
        
        if (gameDataProvider != null)
        {
            Debug.Log($"[SkillManager] UseSkill - GameId: '{gameDataProvider.GameId}'");
            Debug.Log($"[SkillManager] UseSkill - MyPlayerId: '{gameDataProvider.MyPlayerId}'");
        }

        // 既に使用済みかチェック
        if (myUsedSkills.Contains(skillType))
        {
            Debug.LogWarning($"[SkillManager] Skill {skillType} already used!");
            return;
        }

        // Obstructチェック
        if (isPlayerObstructed && skillType != "None")
        {
            Debug.LogWarning("[SkillManager] Player is obstructed - cannot use skills!");
            panelManager?.ShowOpponentUsedSkillNotification("スキルが封じられています");
            return;
        }

        // サーバーに送信
        SendSkillToServer(skillType);
    }

    /// <summary>
    /// サーバーにスキル使用を送信
    /// </summary>
    private void SendSkillToServer(string skillType)
    {
        if (gameDataProvider == null)
        {
            Debug.LogError("[SkillManager] GameDataProvider is null!");
            return;
        }

        string gameId = gameDataProvider.GameId;
        string playerId = gameDataProvider.MyPlayerId;

        Debug.Log($"[SkillManager] DEBUG - GameDataProvider details:");
        Debug.Log($"[SkillManager] - GameId: '{gameId}' (Length: {(gameId?.Length ?? 0)})");
        Debug.Log($"[SkillManager] - MyPlayerId: '{playerId}' (Length: {(playerId?.Length ?? 0)})");
        Debug.Log($"[SkillManager] - GameData is null: {gameDataProvider.GameData == null}");
        
        // テスト用のダミーデータ（実際のデータが読み込まれていない場合）
        if (string.IsNullOrEmpty(gameId))
        {
            Debug.LogWarning("[SkillManager] GameId is empty, using test data");
            gameId = "test_game_123";
        }
        
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("[SkillManager] MyPlayerId is empty, using test data");
            playerId = "test_player_456";
        }

        Debug.Log($"[SkillManager] Sending skill to server - Game: {gameId}, Player: {playerId}, Skill: {skillType}");

        OnePoker.Network.HttpManager.Instance.UseSkill(
            gameId,
            playerId,
            skillType,
            OnSkillUsedSuccess,
            OnSkillUsedError
        );
    }

    /// <summary>
    /// スキル使用成功時のコールバック
    /// </summary>
    private void OnSkillUsedSuccess(OnePoker.Network.HttpManager.UseSkillResponse response)
    {
        Debug.Log($"[SkillManager] Skill used successfully: {response.skillType}");
        Debug.Log($"[SkillManager] Updated used skills: {string.Join(", ", response.usedSkills)}");

        // ローカルの使用済スキルリストを更新
        if (response.usedSkills != null)
        {
            myUsedSkills = response.usedSkills.ToList();
        }

        // Providerにも即時反映（体感改善・フェーズ跨ぎでの確実な表示のため）
        if (gameDataProvider != null && gameDataProvider.GameData != null)
        {
            bool isP1 = gameDataProvider.IsPlayer1;
            var currentP1 = gameDataProvider.GameData.player1UsedSkills ?? new List<string>();
            var currentP2 = gameDataProvider.GameData.player2UsedSkills ?? new List<string>();
            if (isP1)
            {
                gameDataProvider.UpdateUsedSkills(myUsedSkills, currentP2);
            }
            else
            {
                gameDataProvider.UpdateUsedSkills(currentP1, myUsedSkills);
            }
        }

        // スキルボタンの状態を更新
        UpdateSkillButtonStates();

        // スキル効果を適用
        ApplySkillEffect(response.skillType);
    }

    /// <summary>
    /// スキル使用失敗時のコールバック
    /// </summary>
    private void OnSkillUsedError(string error)
    {
        Debug.LogError($"[SkillManager] Failed to use skill: {error}");
        panelManager?.ShowOpponentUsedSkillNotification($"スキル使用エラー: {error}");
    }

    /// <summary>
    /// スキル効果を適用（クライアント側の演出）
    /// </summary>
    private void ApplySkillEffect(string skillType)
    {
        Debug.Log($"[SkillManager] Applying skill effect: {skillType}");

            switch (skillType)
            {
            case "Scan":
                // Scanスキル: 相手の手札を1枚表示
                StartScanSkill();
                break;

            case "Change":
                // Changeスキル: 自分の手札を1枚交換（実装予定）
                panelManager?.ShowOpponentUsedSkillNotification("Changeを使用しました");
                break;

            case "Obstruct":
                // Obstructスキル: 次のターンで相手のスキル使用を禁止
                panelManager?.ShowOpponentUsedSkillNotification("Obstructを使用しました");
                break;

            case "FakeOut":
                // FakeOutスキル: 相手にScanのブラフを送る
                panelManager?.ShowOpponentUsedSkillNotification("FakeOutを使用しました");
                break;

            case "Copy":
                // Copyスキル: 相手が最後に使ったスキルをコピー（実装予定）
                panelManager?.ShowOpponentUsedSkillNotification("Copyを使用しました");
                break;
        }
    }

    /// <summary>
    /// Obstruct状態を設定（相手がObstructを使用した場合）
    /// </summary>
    public void SetPlayerObstructed(bool obstructed)
    {
        isPlayerObstructed = obstructed;
        Debug.Log($"[SkillManager] Player obstruct state set to: {obstructed}");
        
        if (obstructed && panelManager != null)
        {
            panelManager.ShowOpponentUsedSkillNotification("相手のObstructにより、スキルが封じられました");
        }
    }

    /// <summary>
    /// ターン開始時にObstruct状態をチェック（1ターンのみ有効）
    /// </summary>
    private void CheckAndUpdateObstructStatus()
    {
        Debug.Log($"[SkillManager] CheckAndUpdateObstructStatus - obstructTurnCounter: {obstructTurnCounter}, alreadyProcessed: {obstructAlreadyProcessed}");
        
        // 既にObstruct効果を受けている場合
        if (isPlayerObstructed)
        {
            obstructTurnCounter++;
            Debug.Log($"[SkillManager] Obstruct turn counter incremented: {obstructTurnCounter}");
            
            // 1ターン経過したら解除
            if (obstructTurnCounter >= 2)
            {
                SetPlayerObstructed(false);
                obstructTurnCounter = 0;
                obstructAlreadyProcessed = true;  // ← 処理済みフラグを立てる
                Debug.Log("[SkillManager] Obstruct effect ended (1 turn passed)");
                
                if (panelManager != null)
                {
                    panelManager.ShowOpponentUsedSkillNotification("Obstructの効果が解除されました");
                }
            }
            else
            {
                Debug.Log("[SkillManager] Obstruct effect still active (turn 1)");
            }
        }
        // 新たにObstructを受けた場合（まだ処理していない場合のみ）
        else if (opponentUsedSkills.Contains("Obstruct") && !obstructAlreadyProcessed)
        {
            SetPlayerObstructed(true);
            obstructTurnCounter = 1;
            Debug.Log("[SkillManager] New Obstruct effect applied (turn 1 started)");
        }
        else if (opponentUsedSkills.Contains("Obstruct") && obstructAlreadyProcessed)
        {
            Debug.Log("[SkillManager] Obstruct already processed, skipping");
        }
    }

    /// <summary>
    /// 使用済スキルをリセット（新しいゲーム開始時）
    /// </summary>
    public void ResetUsedSkills()
    {
        myUsedSkills.Clear();
        opponentUsedSkills.Clear();
        lastNotifiedOpponentSkills.Clear();
        isPlayerObstructed = false;
        obstructTurnCounter = 0;
        obstructAlreadyProcessed = false;
        Debug.Log("[SkillManager] Used skills reset");
    }

    // ========== Scan スキル実装 ==========
    
    private bool isScanningMode = false;
    private OnlineHandManager handManager;
    
    /// <summary>
    /// Scanスキルを開始（相手の手札を選択させる）
    /// </summary>
    private void StartScanSkill()
    {
        Debug.Log("[SkillManager] Starting Scan skill - waiting for opponent card selection");
        
        // HandManagerを取得
        if (handManager == null)
        {
            handManager = FindObjectOfType<OnlineHandManager>();
        }
        
        if (handManager == null)
        {
            Debug.LogError("[SkillManager] OnlineHandManager not found!");
            return;
        }
        
        isScanningMode = true;
        
        // UIにメッセージを表示
        panelManager?.ShowOpponentUsedSkillNotification("相手の手札を1枚選択してください");
        
        // 相手の手札にクリック可能な状態を設定
        handManager.EnableOpponentCardSelection(this);
        
        Debug.Log("[SkillManager] Scan mode enabled - waiting for user to click on opponent card");
    }
    
    /// <summary>
    /// Scanスキル完了（相手の手札が選択された）
    /// </summary>
    public void OnOpponentCardSelected(GameObject card)
    {
        if (!isScanningMode)
        {
            Debug.LogWarning("[SkillManager] OnOpponentCardSelected called but not in scanning mode");
            return;
        }
        
        Debug.Log($"[SkillManager] Opponent card selected: {card.name}");
        
        // 選択されたカードを表向きにする
        var cardDisplay = card.GetComponent<CardDisplay>();
        if (cardDisplay != null)
        {
            int cardValue = cardDisplay.CardValue;
            Debug.Log($"[SkillManager] Revealing opponent card - CardValue: {cardValue}");
            
            // カードを表向きに表示
            cardDisplay.SetCard(true);
            
            // メッセージを表示
            panelManager?.ShowOpponentUsedSkillNotification($"相手の手札を確認: {GetCardName(cardValue)}");
        }
        
        isScanningMode = false;
        
        // 相手の手札の選択可能状態を解除
        handManager.DisableOpponentCardSelection();
        
        Debug.Log("[SkillManager] Scan skill completed");
    }
    
    /// <summary>
    /// カード名を取得（デバッグ用）
    /// </summary>
    private string GetCardName(int cardValue)
    {
        int rank = cardValue % 13;
        string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        int suit = cardValue / 13;
        string[] suits = { "♠", "♥", "♦", "♣" };
        
        return $"{ranks[rank]} {suits[suit]}";
    }
}
