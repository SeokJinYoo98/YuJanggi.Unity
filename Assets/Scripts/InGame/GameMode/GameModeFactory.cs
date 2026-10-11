using System;
using YuJanggi.Core.InGame;
using YuJanggi.InGame.Input;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiEngine;
using YuJanggi.AI.Data;
using YuJanggi.Store;
using YuJanggi.InGame.Handler;
using YuJanggi.BootStrap;

namespace YuJanggi.InGame.Mode
{
    internal static class GameModeFactory
    {
        public static IGameMode Create(
            GameModeType type,
            IJanggiEngine engine,
            InputHandlerFactory inputs,
            IGameCommandReceiver receiver,
            InGameNetworkHandler networkHandler = null)
            => type switch
            {
                GameModeType.Local => new LocalMode(engine, inputs, receiver),

                GameModeType.AI => new AIMode(
                    engine, inputs, receiver,
                    JanggiOptionStore.JanggiSetting,
                    JanggiOptionStore.AISetting ?? AIMoveStrategyType.Random),

                GameModeType.Network
                    => new NetworkMode(
                        engine,
                        inputs,
                        YuJanggiBootStrap.Instance.NetworkManager,
                        NetworkSessionStore.MyTeam,
                        receiver),

                _ => throw new NotSupportedException(
                    $"아직 지원하지 않는 GameMode입니다: {type}")
            };
    }
}
