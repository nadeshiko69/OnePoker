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

    void Start()
    {
        gameDataProvider = FindObjectOfType<OnlineGameDataProvider>();
        panelManager = FindObjectOfType<OnlinePanelManager>();

        Debug.Log($"[SkillManager] Start() - GameDataProvider found: {gameDataProvider != null}");
        Debug.Log($"[SkillManager] Start() - PanelManager found: {panelManager != null}");
        
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
        
        // AWSから使用済スキルを読み込み
        UpdateUsedSkillsFromServer();
        
        // Obstruct状態をチェック（1ターンのみ有効）
        CheckAndUpdateObstructStatus();

        // スキルUIを表示
        if (panelManager != null)
        {
            panelManager.ShowSkillUI();
            UpdateSkillButtonStates();
        }
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
    }

    /// <summary>
    /// 相手が使用したスキルをUI上に表示
    /// </summary>
    private void UpdateOpponentSkillDisplay()
    {
        if (panelManager == null) return;

        // 相手が使用したスキルを表示
        foreach (string skillType in opponentUsedSkills)
        {
            Debug.Log($"[SkillManager] Opponent used skill: {skillType}");
            
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
                // Scanスキル: 相手の手札を1枚表示（オンライン対戦では演出のみ）
                panelManager?.ShowOpponentUsedSkillNotification("Scanを使用しました");
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
        isPlayerObstructed = false;
        obstructTurnCounter = 0;
        obstructAlreadyProcessed = false;
        Debug.Log("[SkillManager] Used skills reset");
    }
}
