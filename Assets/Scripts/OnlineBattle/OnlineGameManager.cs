using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class OnlineGameManager : MonoBehaviour
{
    public enum SkillType
    {
        Scan,
        Change,
        Obstruct,
        FakeOut,
        Copy,
        None
    }
    private OnlineMatchManager matchManager;
    private OnlineResultViewManager resultViewManager;
    private OnlinePanelManager panelManager;
    private OnlineSkillManager skillManager;
    private OnlineHandManager handManager;

    // プレイヤーの手札・相手の手札
    private int[] myHand;
    private int[] opponentHand;

    // プレイヤー情報
    private bool isPlayer1;
    private string playerId;
    private string opponentId;

    // ライフ
    public TextMeshProUGUI playerLifeText;
    public TextMeshProUGUI opponentLifeText;

    void Start()
    {
        // 各マネージャの取得
        matchManager = FindObjectOfType<OnlineMatchManager>();
        resultViewManager = FindObjectOfType<OnlineResultViewManager>();
        panelManager = FindObjectOfType<OnlinePanelManager>();
        skillManager = FindObjectOfType<OnlineSkillManager>();
        handManager = FindObjectOfType<OnlineHandManager>();

        // OnlineGameDataから手札・プレイヤー情報を取得
        string gameDataJson = PlayerPrefs.GetString("OnlineGameData", "");
        if (!string.IsNullOrEmpty(gameDataJson))
        {
            var gameData = JsonUtility.FromJson<OnlineGameDataWithCards>(gameDataJson);
            if (gameData != null && handManager != null)
            {
                isPlayer1 = gameData.isPlayer1;
                playerId = gameData.playerId;
                opponentId = gameData.opponentId;

                // 手札をセット
                myHand = isPlayer1 ? gameData.player1Cards : gameData.player2Cards;
                opponentHand = isPlayer1 ? gameData.player2Cards : gameData.player1Cards;

                handManager.SetPlayerHand(myHand);
                handManager.SetOpponentHand(opponentHand);
            }
        }

        // ライフUI初期化
        UpdateLifeUI();
    }

    // ライフUIの更新
    public void UpdateLifeUI()
    {
        if (playerLifeText != null)
            playerLifeText.text = $"Life: {matchManager.PlayerLife}";
        if (opponentLifeText != null)
            opponentLifeText.text = $"Life: {matchManager.OpponentLife}";
    }

    // カード配置・スキル・ベットなどのイベントは
    // サーバー同期用のメソッドをここに追加していく
    // 例: public void PlaceCard(int cardId) { ... }
    // 例: public void UseSkill(SkillType skill) { ... }

    [System.Serializable]
    private class OnlineGameDataWithCards
    {
        public string roomCode;
        public string playerId;
        public string opponentId;
        public bool isPlayer1;
        public string gameId;
        public int[] player1Cards;
        public int[] player2Cards;
    }

    
    public void ConfirmPlacement() { /* TODO: 実装 */ }
    public void CancelPlacement() { /* TODO: 実装 */ }
    public void PlaceBet(int amount) { /* TODO: 実装 */ }
    public int CurrentBetAmount { get; private set; }
    public void SetOpponentCalled(bool called) { /* TODO: 実装 */ }
    public void RevealCards() { /* TODO: 実装 */ }
    public CardDisplay SetPlayerCard { get; }
    public CardDisplay SetOpponentCard { get; }
    public bool PlayerCanUseScanSkill { get; }
    public bool PlayerCanUseChangeSkill { get; }
    public bool PlayerCanUseObstructSkill { get; }
    public bool PlayerCanUseFakeOutSkill { get; }
    public bool PlayerCanUseCopySkill { get; }
}
