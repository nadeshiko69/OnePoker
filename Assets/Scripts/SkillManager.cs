using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SkillManager : MonoBehaviour
{
    private PanelManager panelManager;
    private DeckManager deckManager;
    private GameManager gameManager;

    void Start()
    {
        panelManager = FindObjectOfType<PanelManager>();
        deckManager = FindObjectOfType<DeckManager>();
        gameManager = FindObjectOfType<GameManager>();
    }

    public void UseSkill(GameManager.SkillType skillType)
    {
        Debug.Log("UseSkill called");
        switch (skillType)
        {
            case GameManager.SkillType.Scan: StartCoroutine(ScanSkill()); break;
            case GameManager.SkillType.Change: ChangeSkill(); break;
            case GameManager.SkillType.Obstruct: ObstructSkill(); break;
            case GameManager.SkillType.FakeOut: FakeOutSkill(); break;
            case GameManager.SkillType.Copy: CopySkill(); break;
        }
    }

    

    public IEnumerator ScanSkill()
    {
        Debug.Log("ScanSkill called");
        panelManager.descriptionSkillPanel.SetActive(false);

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
        
        // Scanを使用済に変更
        panelManager.SetSkillButtonInteractable(false);
        gameManager.SetSkillAvailability(GameManager.PlayerType.Player, GameManager.SkillType.Scan, false);
   }

    public void ChangeSkill()
    {
        Debug.Log("ChangeSkill called");
        panelManager.ShowDescriptionSkillPanel(GameManager.SkillType.Change, "交換するカードを選択してください。");
        panelManager.VisibleChangeCardButtons(true);
        panelManager.ChangeCard1Button.onClick.AddListener(() => OnCardSelected(deckManager.playerCard1, deckManager.playerUI1, "Player_Card1", 1));
        panelManager.ChangeCard2Button.onClick.AddListener(() => OnCardSelected(deckManager.playerCard2, deckManager.playerUI2, "Player_Card2", 2));

        // Changeを使用済に変更
        panelManager.SetSkillButtonInteractable(false);
        gameManager.SetSkillAvailability(GameManager.PlayerType.Player, GameManager.SkillType.Change, false);
    }

    public void OnCardSelected(CardDisplay card, TextMeshProUGUI resultText, string name, int cardIndex)
    {
        Debug.Log("OnCardSelected called");
        panelManager.descriptionSkillPanel.SetActive(false);
        panelManager.VisibleChangeCardButtons(false);

        Destroy(card.gameObject);
        deckManager.RefillPlayerCard();
    }

    public void ObstructSkill()
    {
        Debug.Log("ObstructSkill called");

        // Obstructを使用済に変更
        gameManager.SetSkillAvailability(GameManager.PlayerType.Player, GameManager.SkillType.Obstruct, false);
        panelManager.SetSkillButtonInteractable(false);
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
    
    
}
