using UnityEngine;

public class TitleManager : MonoBehaviour
{
    void Start(){
        registerAccountPanel.SetActive(false);
    }

    // アカウント登録
    public GameObject registerAccountPanel;
}
