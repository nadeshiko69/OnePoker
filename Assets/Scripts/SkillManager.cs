using UnityEngine;
using UnityEngine.UI;

public class SkillManager : MonoBehaviour
{
    private PanelManager panelManager;

    void Start()
    {
        panelManager = FindObjectOfType<PanelManager>();
    }

    public void UseSkill(string skillName)
    {
        Debug.Log("UseSkill called");
        switch (skillName)
        {
            case "Scan": ScanSkill(); break;
            case "Change": ChangeSkill(); break;
            case "Obstruct": ObstructSkill(); break;
            case "FakeOut": FakeOutSkill(); break;
            case "Copy": CopySkill(); break;
        }
    }

    public void ScanSkill()
    {
        Debug.Log("ScanSkill called");
        panelManager.ShowDescriptionSkillPanel("Scan", panelManager.DescriptionScanSkill);
    }

    public void ChangeSkill()
    {
        Debug.Log("ChangeSkill called");
        panelManager.ShowDescriptionSkillPanel("Change", panelManager.DescriptionChangeSkill);
    }

    public void ObstructSkill()
    {
        Debug.Log("ObstructSkill called");
        panelManager.ShowDescriptionSkillPanel("Obstruct", panelManager.DescriptionObstructSkill);
    }
    
    public void FakeOutSkill()
    {
        Debug.Log("FakeOutSkill called"); 
        panelManager.ShowDescriptionSkillPanel("FakeOut", panelManager.DescriptionFakeOutSkill);
    }

    public void CopySkill()
    {
        Debug.Log("CopySkill called");
        panelManager.ShowDescriptionSkillPanel("Copy", panelManager.DescriptionCopySkill);
    }
    
    
}
