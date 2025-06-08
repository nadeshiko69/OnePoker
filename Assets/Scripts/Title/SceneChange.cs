using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChange : MonoBehaviour
{

    void Start(){
        registerAccountPanel.SetActive(false);
    }


    // アカウント登録
    public GameObject registerAccountPanel;

    public void OpenRegisterAccountPanel(){
        registerAccountPanel.SetActive(true);
    }

    public void CloseRegisterAccountPanel(){
        registerAccountPanel.SetActive(false);
    }

    // シーン切り替え
    public void ChangeRankMatchScene(){
        SceneManager.LoadScene("RankMatchScene");
    }

    public void ChangeCPUBattleScene(){
        SceneManager.LoadScene("CpuBattleScene");
    }

    public void ChangeSettingScene(){
        SceneManager.LoadScene("SettingScene");
    }
}