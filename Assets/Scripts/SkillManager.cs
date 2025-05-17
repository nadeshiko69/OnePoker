using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SkillManager : MonoBehaviour
{
    private PanelManager panelManager;
    private DeckManager deckManager;

    void Start()
    {
        panelManager = FindObjectOfType<PanelManager>();
        deckManager = FindObjectOfType<DeckManager>();
    }

    public void UseSkill(string skillName)
    {
        Debug.Log("UseSkill called");
        switch (skillName)
        {
            case "Scan": StartCoroutine(ScanSkill()); break;
            case "Change": ChangeSkill(); break;
            case "Obstruct": ObstructSkill(); break;
            case "FakeOut": FakeOutSkill(); break;
            case "Copy": CopySkill(); break;
        }
    }

    

    public IEnumerator ScanSkill()
    {
        Debug.Log("ScanSkill called");
        
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
        panelManager.ShowDescriptionSkillPanel("Change", "交換するカードを選択してください。");
        panelManager.VisibleChangeCardButtons(true);
        panelManager.ChangeCard1Button.onClick.AddListener(() => OnCardSelected(deckManager.playerCard1, deckManager.playerUI1, "Player_Card1", 1));
        panelManager.ChangeCard2Button.onClick.AddListener(() => OnCardSelected(deckManager.playerCard2, deckManager.playerUI2, "Player_Card2", 2));

    }

    public void OnCardSelected(CardDisplay card, TextMeshProUGUI resultText, string name, int cardIndex)
    {
        Debug.Log("OnCardSelected called");
        panelManager.VisibleChangeCardButtons(false);

        Destroy(card.gameObject);
        deckManager.RefillPlayerCard();
    }

    public void ObstructSkill()
    {
        Debug.Log("ObstructSkill called");
    }
    
    public void FakeOutSkill()
    {
        Debug.Log("FakeOutSkill called"); 
    }

    public void CopySkill()
    {
        Debug.Log("CopySkill called");
    }
    
    
}
