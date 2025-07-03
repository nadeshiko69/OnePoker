// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class OnlineBattleInitializer : MonoBehaviour
// {
//     [SerializeField] private OnlineGameManager onlineGameManager;
    
//     void Start()
//     {
//         // PlayerPrefsからゲームデータを読み込み
//         string gameDataJson = PlayerPrefs.GetString("OnlineGameData", "");
        
//         if (string.IsNullOrEmpty(gameDataJson))
//         {
//             Debug.LogError("OnlineGameData not found in PlayerPrefs");
//             // エラー時はタイトルシーンに戻る
//             SceneManager.LoadScene("TitleScene");
//             return;
//         }
        
//         try
//         {
//             var gameData = JsonUtility.FromJson<OnlineGameData>(gameDataJson);
            
//             // OnlineGameManagerを初期化
//             if (onlineGameManager != null)
//             {
//                 onlineGameManager.StartOnlineGame(
//                     gameData.roomCode,
//                     gameData.playerId,
//                     gameData.opponentId,
//                     gameData.isPlayer1,
//                     gameData.gameId
//                 );
//             }
//             else
//             {
//                 Debug.LogError("OnlineGameManager not assigned");
//             }
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogError($"Failed to parse OnlineGameData: {e.Message}");
//             SceneManager.LoadScene("TitleScene");
//         }
//     }
    
//     /// <summary>
//     /// タイトルシーンに戻る
//     /// </summary>
//     public void ReturnToTitle()
//     {
//         // PlayerPrefsからゲームデータを削除
//         PlayerPrefs.DeleteKey("OnlineGameData");
//         PlayerPrefs.Save();
        
//         SceneManager.LoadScene("TitleScene");
//     }
    
//     [System.Serializable]
//     private class OnlineGameData
//     {
//         public string roomCode;
//         public string playerId;
//         public string opponentId;
//         public bool isPlayer1;
//         public string gameId;
//     }
// } 