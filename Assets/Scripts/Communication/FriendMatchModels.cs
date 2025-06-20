using System;

namespace OnePoker.FriendMatch
{
    // --- Request Payloads ---

    [Serializable]
    public class CreateRoomRequest
    {
        public string playerId;
    }

    [Serializable]
    public class JoinRoomRequest
    {
        public string code;
        public string guestPlayerId;
    }

    [Serializable]
    public class CancelRoomRequest
    {
        public string roomcode;
    }

    // --- Response Payloads ---

    [Serializable]
    public class CreateRoomResponse
    {
        public string code;
    }
    
    [Serializable]
    public class SuccessResponse
    {
        public string message;
    }
    
    [Serializable]
    public class ErrorResponse
    {
        public string error;
        public string message; // for join_room error
    }
} 