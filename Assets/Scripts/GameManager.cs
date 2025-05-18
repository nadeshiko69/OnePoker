using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    private MatchManager matchManager;
    private DeckManager deckManager;
    private ResultViewManager resultViewManager;
    private PanelManager panelManager;

    private CardDisplay currentCard;
    private DropZone currentZone;

    // For Debug ; 相手のカードを自動で配置
    private bool opponent_setCard = false;
    private CardDisplay opponentCard;
    public DropZone opponentZone;

    // プレイヤーのカードを保持
    private CardDisplay setPlayerCard;
    private CardDisplay setOpponentCard;
    public CardDisplay SetPlayerCard => setPlayerCard;
    public CardDisplay SetOpponentCard => setOpponentCard;

    // スキル使用可能フラグ
    private bool playerCanUseScanSkill = true;
    private bool playerCanUseChangeSkill = true;
    private bool playerCanUseObstructSkill = true;
    private bool playerCanUseFakeOutSkill = true;
    private bool playerCanUseCopySkill = true;
    public bool PlayerCanUseScanSkill => playerCanUseScanSkill;
    public bool PlayerCanUseChangeSkill => playerCanUseChangeSkill;
    public bool PlayerCanUseObstructSkill => playerCanUseObstructSkill;
    public bool PlayerCanUseFakeOutSkill => playerCanUseFakeOutSkill;
    public bool PlayerCanUseCopySkill => playerCanUseCopySkill;

    private bool opponentCanUseScanSkill = true;
    private bool opponentCanUseChangeSkill = true;
    private bool opponentCanUseObstructSkill = true;
    private bool opponentCanUseFakeOutSkill = true;
    private bool opponentCanUseCopySkill = true;
    public bool OpponentCanUseScanSkill => opponentCanUseScanSkill;
    public bool OpponentCanUseChangeSkill => opponentCanUseChangeSkill;
    public bool OpponentCanUseObstructSkill => opponentCanUseObstructSkill;
    public bool OpponentCanUseFakeOutSkill => opponentCanUseFakeOutSkill;
    public bool OpponentCanUseCopySkill => opponentCanUseCopySkill;

    public enum SkillType
    {
        Scan,
        Change,
        Obstruct,
        FakeOut,
        Copy
    }

    public enum PlayerType
    {
        Player,
        Opponent
    }

    // 両者カードを配置したらベット開始
    private bool bothCardsPlaced = false;

    // ベットフェーズ用のUI
    public TextMeshProUGUI playerLifeText;
    public TextMeshProUGUI opponentLifeText;
    private int currentBetAmount = 0;
    public int CurrentBetAmount => currentBetAmount;

    private bool OpponentCalled = false;
    private bool cardsRevealed = false;

    private bool checkGameOver = false;
    public bool CheckGameOver => checkGameOver;

    void Start()
    {
        deckManager = FindObjectOfType<DeckManager>();
        resultViewManager = FindObjectOfType<ResultViewManager>();
        matchManager = FindObjectOfType<MatchManager>();
        panelManager = FindObjectOfType<PanelManager>();
    }

    void Update()
    {
        if (opponent_setCard)
        {
            // ランダムに相手のカードを選択
            opponentCard = GetRandomOpponentCard();
            PlaceOpponentCard(opponentCard, opponentZone);
            opponent_setCard = false;
        }

        if (bothCardsPlaced)
        {
            Debug.Log("ベット開始");
            panelManager.ShowBettingUI();
            bothCardsPlaced = false;
        }
    }

    // 次のGameを開始する前に状態をリセット
    public void ResetGame(){
        checkGameOver = false;
        bothCardsPlaced = false;
        OpponentCalled = false;
        cardsRevealed = false;   

        panelManager.SetSkillButtonInteractable(true);
    }

    private IEnumerator SetOpponentCardFlag()
    {
        yield return new WaitForSeconds(1f);
        opponent_setCard = true;
    }
    
    private IEnumerator SetBettingPhase()
    {
        yield return new WaitForSeconds(1f);
        bothCardsPlaced = true;
    }

    public void SetCheckGameOver(bool check){
        checkGameOver = check;
    }


    public void SetSkillAvailability(PlayerType player, SkillType skill, bool canUse)
    {
        switch (player)
        {
            case PlayerType.Player:
                switch (skill)
                {
                    case SkillType.Scan: playerCanUseScanSkill = canUse; break;
                    case SkillType.Change: playerCanUseChangeSkill = canUse; break;
                    case SkillType.Obstruct: playerCanUseObstructSkill = canUse; break;
                    case SkillType.FakeOut: playerCanUseFakeOutSkill = canUse; break;
                    case SkillType.Copy: playerCanUseCopySkill = canUse; break;
                }
                break;
            case PlayerType.Opponent:
                switch (skill)
                {
                    case SkillType.Scan: opponentCanUseScanSkill = canUse; break;
                    case SkillType.Change: opponentCanUseChangeSkill = canUse; break;
                    case SkillType.Obstruct: opponentCanUseObstructSkill = canUse; break;
                    case SkillType.FakeOut: opponentCanUseFakeOutSkill = canUse; break;
                    case SkillType.Copy: opponentCanUseCopySkill = canUse; break;
                }
                break;
        }
    }

    private void UpdateLifeUI()
    {
        if (playerLifeText != null)
        {
            playerLifeText.text = $"Life: {matchManager.PlayerLife}";
        }
        if (opponentLifeText != null)
        {
            opponentLifeText.text = $"Life: {matchManager.OpponentLife}";
        }
    }

    public void PlaceBet(int amount)
    {
        if (amount > 0)
        {
            // ベット額を1増やす
            if (matchManager.PlayerLife >= 1) // 1ライフ以上あればベット可能
            {
                currentBetAmount += 1;
                matchManager.UpdatePlayerLife(-1); // 1ずつライフを減らす
                Debug.Log($"Betting {currentBetAmount} life!");
                UpdateLifeUI();
                panelManager.UpdateCallButtonText();
            }
            else
            {
                Debug.LogWarning("ライフが足りないためベットできません！");
            }
        }
        else if (amount < 0)
        {
            // ベット額を1減らす
            if (currentBetAmount > 1) // 最小ベット額は1
            {
                currentBetAmount -= 1;
                matchManager.UpdatePlayerLife(1); // ライフを1戻す
                Debug.Log($"Reducing bet to {currentBetAmount} life!");
                UpdateLifeUI();
                panelManager.UpdateCallButtonText();
            }
        }
    }

    public void RevealCards()
    {
        if (!cardsRevealed)
        {
            // プレイヤーのカードを表向きにする
            Debug.Log("RevealCards called");
            // プレイヤーのカードはすでに表向きなのでSetCard不要
            // 相手のカードを表向きにする
            if (opponentCard != null)
            {
                CardDisplay opponentCardDisplay = opponentCard.GetComponent<CardDisplay>();
                if (opponentCardDisplay != null)
                {
                    opponentCardDisplay.SetCard(true);
                }
            }

            cardsRevealed = true;

            // 勝敗判定を表示
            if (deckManager != null)
            {
                if (resultViewManager == null)
                {
                    resultViewManager = FindObjectOfType<ResultViewManager>();
                    if (resultViewManager == null)
                    {
                        Debug.LogError("ResultViewManager not found in the scene!");
                        return;
                    }
                }
                StartCoroutine(ShowResultWithDelay(deckManager));
            }
            else
            {
                Debug.LogError("deckManager not found!");
            }
        }
    }

    private IEnumerator ShowResultWithDelay(DeckManager deckManager)
    {
        if (resultViewManager == null || deckManager == null)
        {
            Debug.LogError("Required components are missing for showing results!");
            yield break;
        }

        yield return new WaitForSeconds(1f);

        if (setPlayerCard != null)
        {
            int playerCardValue = setPlayerCard.GetComponent<CardDisplay>().CardValue;
            int opponentCardValue = setOpponentCard.GetComponent<CardDisplay>().CardValue;
            Debug.Log($"Showing result - Player: {playerCardValue}, Opponent: {opponentCardValue}");
            panelManager.ShowResultPanel(playerCardValue, opponentCardValue);
            resultViewManager.ShowResultTable(playerCardValue, opponentCardValue);
            UpdateLife(playerCardValue, opponentCardValue);
        }
        else
        {
            Debug.LogError("No player cards found!");
        }
        
        yield return new WaitForSeconds(3f);
        panelManager.HidePanel(panelManager.resultPanel);

        // 結果を表示したらベットボタン→スキルボタン
        panelManager.ShowSkillUI();

        matchManager.OnGameComplete();
    }

    // 1回の勝負が終わった後に残ライフを更新する
    private void UpdateLife(int playerValue, int opponentValue)
    {
        Debug.Log($"Before result - Player Life: {matchManager.PlayerLife}, Opponent Life: {matchManager.OpponentLife}, Bet: {currentBetAmount}");

        if (playerValue == opponentValue)
        {
            // 引き分けの場合は両者のライフを元に戻す
            matchManager.UpdatePlayerLife(currentBetAmount);
            matchManager.UpdateOpponentLife(currentBetAmount);
            Debug.Log("Draw - Returning bets");
        }
        else if (resultViewManager.IsWinner(playerValue, opponentValue))
        {
            // プレイヤーの勝利
            matchManager.UpdatePlayerLife(currentBetAmount*2);
            Debug.Log($"Player wins - Player gains opponent's bet: {currentBetAmount*2}");
        }
        else
        {
            // 相手の勝利
            matchManager.UpdateOpponentLife(currentBetAmount*2);
            Debug.Log($"CPU wins - CPU gains player's bet: {currentBetAmount*2}");
        }

        Debug.Log($"After result - Player Life: {matchManager.PlayerLife}, Opponent Life: {matchManager.OpponentLife}");

        checkGameOver = true;
    }

    public void ShowConfirmation(CardDisplay card, DropZone zone)
    {
        Debug.Log("ShowConfirmation called");
        Debug.Log("number: " + card.GetComponent<CardDisplay>().CardValue);
        currentCard = card;
        currentZone = zone;
        panelManager.confirmationPanel.SetActive(true);

        // リスナーの多重登録防止
        panelManager.yesButton.onClick.RemoveAllListeners();
        panelManager.noButton.onClick.RemoveAllListeners();

        panelManager.yesButton.onClick.AddListener(ConfirmPlacement);
        panelManager.noButton.onClick.AddListener(CancelPlacement);
    }

    public void ConfirmPlacement()
    {
        currentCard.transform.position = currentZone.transform.position;
        panelManager.confirmationPanel.SetActive(false);

        // プレイヤーのカードをリストに追加
        setPlayerCard = currentCard;

        ResetState();
        StartCoroutine(SetOpponentCardFlag());
    }

    public void PlaceOpponentCard(CardDisplay card, DropZone zone)
    {
        if (card != null && zone != null)
        {
            // カードをDropZoneの子にする（親子関係をセット）
            card.transform.SetParent(zone.transform);

            // カードの位置を整える（中央揃え）
            card.transform.localPosition = Vector3.zero;

            // カードの裏面を表示
            CardDisplay cardDisplay = card.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                cardDisplay.SetCard(false);
            }
            setOpponentCard = card;
        }
        else
        {
            Debug.LogError("Card or zone is null!");
        }

        StartCoroutine(SetBettingPhase());
    }

    public void CancelPlacement()
    {
        // カードを元の位置に戻す
        var drag = currentCard.GetComponent<CardDraggable>();
        if (drag != null)
        {
            currentCard.transform.SetParent(drag.OriginalParent);
            currentCard.transform.position = drag.OriginalPosition;
        }
        
        // パネル非表示 & 状態クリア
        panelManager.confirmationPanel.SetActive(false);
        ResetState();
    }

    public void SetOpponentCalled(bool called)
    {
        OpponentCalled = called;
    }

    private void ResetState()
    {
        currentCard = null;
        currentZone = null;
        panelManager.confirmationPanel.SetActive(false);
        OpponentCalled = false;
        cardsRevealed = false;
        currentBetAmount = 0;
        panelManager.UpdateCallButtonText();

        // リスナーをリセット
        panelManager.yesButton.onClick.RemoveAllListeners();
        panelManager.noButton.onClick.RemoveAllListeners();
    }





    //////////////////////////////////////
    ////////// CPUのランダム要素 //////////
    //////////////////////////////////////

    // CPUのセットするカードをランダムに選択
    private CardDisplay GetRandomOpponentCard()
    {
        int randomIndex = Random.Range(0, 2);
        return randomIndex == 0 ? deckManager.opponentCard1 : deckManager.opponentCard2;
    }

    // CPUがターン中に使用するスキルをランダムに選択
    private SkillType GetRandomSkill()
    {
        int randomIndex = Random.Range(0, 5);
        return (SkillType)randomIndex;
    }
}
