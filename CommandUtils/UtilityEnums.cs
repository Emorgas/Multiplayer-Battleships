using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandUtils
{
    public enum CommandType
    {
        UserConnected,
        UserDataInform,
        UserDisconnectRequest,
        UserDisconnected,
        ClientListRequest,
        UsernameRequest,
        Message,
        ChallengeRequest,
        ChallengeResponse,
        GameStartRequest,
        GameStartInform,
        GameIDInform,
        GameShipRequest,
        GameShipResult,
        GameShotRequest,
        GameShotResult,
        GameHitInform,
        GameMissInform,
        GameOverInform
    }

    public enum ShipType
    {
        Destroyer = 0,
        Cruiser = 1,
        Submarine = 2,
        Battleship = 3,
        Carrier = 4,
        Default = 5
    }

    public enum GridType
    {
        Water = 0,
        Ship = 1,
        Miss = 2,
        Hit = 3
    }
}
