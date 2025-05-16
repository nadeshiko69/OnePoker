using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
