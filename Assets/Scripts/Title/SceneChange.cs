using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChange : MonoBehaviour
{
    private TitleManager titleManager;

    void Start(){
        titleManager = FindObjectOfType<TitleManager>();
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

    // アカウント登録パネル操作
    public void OpenRegisterAccountPanel(){
        titleManager.registerAccountPanel.SetActive(true);
    }
    public void CloseRegisterAccountPanel(){
        titleManager.registerAccountPanel.SetActive(false);
    }

    // アカウント登録処理
    public void RegisterAccount(){
        // パネル上に入力したメールアドレスとユーザ名を使用してCognitoでユーザ登録
        // ユーザ名、メールアドレスのどちらかが登録済なら登録させない
        // 登録に成功したら成功メッセージを表示
        // 登録に失敗したらエラーメッセージを表示
    }   
}