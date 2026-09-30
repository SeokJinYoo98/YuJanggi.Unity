namespace YuJanggi.InGame.Flow
{
    using System;
    using YuJanggi.Engine.Domain;
    using YuJanggi.InGame.Handler;
    using YuJanggi.InGame.Service;
    using YuJanggi.InGame.Session;

    internal static class InGameFlowFactory
    {
        internal static IInGameFlow CreateLocal(
            GameSession session)
            => new LocalInGameFlow(session);

        internal static IInGameFlow CreateNetwork(
            GameSession session,
            InGameHandler handler,
            PlayerTeam localTeam)
            => new NetworkInGameFlow(
                session,
                handler,
                localTeam);
    }
}


