using System;
using UnityEngine;
using OnePoker.Network; // HttpManagerのnamespaceをインポート

namespace OnePoker.FriendMatch
{
    /// <summary>
    /// AWS API Gatewayと通信し、フレンド対戦のマッチング機能を提供します。
    /// </summary>
    public class FriendMatchClient : MonoBehaviour
    {
        [Header("API Gatewayの呼び出しURL")]
        [SerializeField]
        private string apiEndpoint; // 例: https://xxxxxxxxx.execute-api.ap-northeast-1.amazonaws.com/v1

        #region Public Methods

        /// <summary>
        /// 新しいルームを作成します。
        /// </summary>
        /// <param name="playerId">ホストプレイヤーのID</param>
        /// <param name="onSuccess">成功時のコールバック（ルームコードが返る）</param>
        /// <param name="onError">失敗時のコールバック（エラーメッセージが返る）</param>
        public void CreateRoom(string playerId, Action<CreateRoomResponse> onSuccess, Action<string> onError)
        {
            var requestData = new CreateRoomRequest { playerId = playerId };
            var url = apiEndpoint + "/createroom";
            HttpManager.Instance.Post(url, JsonUtility.ToJson(requestData), onSuccess, onError);
        }

        /// <summary>
        /// 既存のルームに参加します。
        /// </summary>
        /// <param name="roomCode">ルームコード</param>
        /// <param name="guestPlayerId">ゲストプレイヤーのID</param>
        /// <param name="onSuccess">成功時のコールバック</param>
        /// <param name="onError">失敗時のコールバック</param>
        public void JoinRoom(string roomCode, string guestPlayerId, Action<SuccessResponse> onSuccess, Action<string> onError)
        {
            var requestData = new JoinRoomRequest { code = roomCode, guestPlayerId = guestPlayerId };
            var url = apiEndpoint + "/joinroom";
            HttpManager.Instance.Post(url, JsonUtility.ToJson(requestData), onSuccess, onError);
        }

        /// <summary>
        /// ルームをキャンセル（削除）します。
        /// </summary>
        /// <param name="roomCode">ルームコード</param>
        /// <param name="onSuccess">成功時のコールバック</param>
        /// <param name="onError">失敗時のコールバック</param>
        public void CancelRoom(string roomCode, Action<SuccessResponse> onSuccess, Action<string> onError)
        {
            var requestData = new CancelRoomRequest { roomcode = roomCode };
            var url = apiEndpoint + "/cancelroom";
            HttpManager.Instance.Post(url, JsonUtility.ToJson(requestData), onSuccess, onError);
        }

        #endregion
    }
} 