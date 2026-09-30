#nullable enable
using System;

namespace YuJanggi.Lobby.Flow
{
    internal interface ILobbyFlow
    {
        event Action<LobbyGameStartContext>? GameStartReady;
        void BindEvents();
        void UnBindEvents();
    }
}
