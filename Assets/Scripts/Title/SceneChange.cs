using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class SceneChange : MonoBehaviour
{
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