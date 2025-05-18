using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SkillManager : MonoBehaviour
{
    private PanelManager panelManager;
    private DeckManager deckManager;
    private GameManager gameManager;

    private bool isPlayerObstructed = false;
    public bool IsPlayerObstructed => isPlayerObstructed;
    private bool isOpponentObstructed = false;
    public bool IsOpponentObstructed => isOpponentObstructed;

    void Start()
    {
        panelManager = FindObjectOfType<PanelManager>();
        deckManager = FindObjectOfType<DeckManager>();
        gameManager = FindObjectOfType<GameManager>();
    }

    public void UseSkill(GameManager.SkillType skillType)
    {
        Debug.Log("UseSkill called");
        Debug.Log("skillType: " + skillType);
        switch (skillType)
        {
            case GameManager.SkillType.Scan: StartCoroutine(ScanSkill()); break;
            case GameManager.SkillType.Change: ChangeSkill(); break;
            case GameManager.SkillType.Obstruct: StartCoroutine(ObstructSkill()); break;
            case GameManager.SkillType.FakeOut: FakeOutSkill(); break;
            case GameManager.SkillType.Copy: CopySkill(); break;
        }
    }

    

    public IEnumerator ScanSkill()
    {
        Debug.Log("ScanSkill called");
        panelManager.descriptionSkillPanel.SetActive(false);

        // Scanを使用済に変更
        panelManager.SetSkillButtonInteractable(false);
        gameManager.SetSkillAvailability(GameManager.PlayerType.Player, GameManager.SkillType.Scan, false);

        // 5sec中にスキル追加できないよう、使用済にしてから相手のカードを表示
        int randomIndex = Random.Range(0, 2);
        CardDisplay opponentCard;
        if (randomIndex == 0){
            opponentCard = deckManager.opponentCard1;
        }
        else{
            opponentCard = deckManager.opponentCard2;
        }

        if (opponentCard != null)
        {
            CardDisplay opponentCardDisplay = opponentCard.GetComponent<CardDisplay>();
            if (opponentCardDisplay != null)
            {
                opponentCardDisplay.SetCard(true);
                yield return new WaitForSeconds(5f);
                opponentCardDisplay.SetCard(false);
            }
            else{
                Debug.LogError("opponentCardDisplay is null");
            }
        }
        else{
            Debug.LogError("opponentCard is null");
        }
   }

    public void ChangeSkill()
    {
        Debug.Log("ChangeSkill called");
        panelManager.ShowDescriptionSkillPanel(GameManager.SkillType.Change, "交換するカードを選択してください。");
        panelManager.VisibleChangeCardButtons(true);
        panelManager.ChangeCard1Button.onClick.AddListener(() => OnCardSelected(deckManager.playerCard1, "Player_Card1", 1));
        panelManager.ChangeCard2Button.onClick.AddListener(() => OnCardSelected(deckManager.playerCard2, "Player_Card2", 2));

        // Changeを使用済に変更
        panelManager.SetSkillButtonInteractable(false);
        gameManager.SetSkillAvailability(GameManager.PlayerType.Player, GameManager.SkillType.Change, false);
    }

    public void OnCardSelected(CardDisplay card, string name, int cardIndex)
    {
        Debug.Log("OnCardSelected called");
        panelManager.descriptionSkillPanel.SetActive(false);
        panelManager.VisibleChangeCardButtons(false);

        Destroy(card.gameObject);
        deckManager.RefillPlayerCard();
    }

    public IEnumerator ObstructSkill()
    {
        Debug.Log("ObstructSkill called");
        panelManager.ShowDescriptionSkillPanel(GameManager.SkillType.Obstruct, "次のターンの相手のスキル使用を禁止しました。");
        panelManager.VisibleSkillSelectButtons(false);
        yield return new WaitForSeconds(5f);
        panelManager.descriptionSkillPanel.SetActive(false);

        // 相手のスキル使用を禁止


        // Obstructを使用済に変更
        panelManager.SetSkillButtonInteractable(false);
        gameManager.SetSkillAvailability(GameManager.PlayerType.Player, GameManager.SkillType.Obstruct, false);
    }

    public void SetPlayerObstructed(bool obstructed)
    {
        isPlayerObstructed = obstructed;
    }

    public void SetOpponentObstructed(bool obstructed)
    {
        isOpponentObstructed = obstructed;
    }
    
    public void FakeOutSkill()
    {
        Debug.Log("FakeOutSkill called"); 

        // FakeOutを使用済に変更
        gameManager.SetSkillAvailability(GameManager.PlayerType.Player, GameManager.SkillType.FakeOut, false);
        panelManager.SetSkillButtonInteractable(false);
    }

    public void CopySkill()
    {
        Debug.Log("CopySkill called");

        // Copyを使用済に変更
        gameManager.SetSkillAvailability(GameManager.PlayerType.Player, GameManager.SkillType.Copy, false);
        panelManager.SetSkillButtonInteractable(false);
    }
    
    

    ////////////////////////////////
    ////////// CPUのスキル //////////
    ////////////////////////////////

    // public bool test = true;
    public void UseOpponentSkill()
    {
        GameManager.SkillType skillType = GameManager.SkillType.None;
        Debug.Log("UseOpponentSkill called");

        int randomIndex = Random.Range(0, 5);
        if (randomIndex < 5){
            skillType = (GameManager.SkillType)randomIndex;
        }
        else{
            skillType = GameManager.SkillType.None;
        }

        Debug.Log("skillType: " + skillType);
        switch (skillType)
        {
            case GameManager.SkillType.Scan: if(gameManager.OpponentCanUseScanSkill)OpponentScanSkill(); break;
            case GameManager.SkillType.Change: if(gameManager.OpponentCanUseChangeSkill)OpponentChangeSkill(); break;
            case GameManager.SkillType.Obstruct: if(gameManager.OpponentCanUseObstructSkill)OpponentObstructSkill(); break;
            case GameManager.SkillType.FakeOut: if(gameManager.OpponentCanUseFakeOutSkill)OpponentFakeOutSkill(); break;
            case GameManager.SkillType.Copy: if(gameManager.OpponentCanUseCopySkill)OpponentCopySkill(); break;
            case GameManager.SkillType.None: Debug.Log("None"); break;
        }

        // テスト用
        // if (test){
        //     if(gameManager.OpponentCanUseObstructSkill)OpponentObstructSkill();
        //     test = false;
        // }
    }

    public void OpponentScanSkill()
    {
        Debug.Log("OpponentScanSkill called");
        // 対人用スキルのため実装不要
    }

    public void OpponentChangeSkill()
    {
        Debug.Log("OpponentChangeSkill called");
        Destroy(deckManager.opponentCard1.gameObject);
        deckManager.RefillOpponentCard();
    }

    public void OpponentObstructSkill()
    {
        Debug.Log("OpponentObstructSkill called");
        isPlayerObstructed = true;
    }

    public void OpponentFakeOutSkill()
    {
        Debug.Log("OpponentFakeOutSkill called");
        // 対人用スキルのため実装不要
    }

    public void OpponentCopySkill()
    {
        Debug.Log("OpponentCopySkill called");
    }
}
