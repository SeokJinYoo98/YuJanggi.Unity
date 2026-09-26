namespace YuJanggi.InGame.Flow
{
    using System;
    using YuJanggi.Core.V2.Domain;
    using YuJanggi.InGame.Handler;
    using YuJanggi.InGame.Service;
    using YuJanggi.InGame.Session;

    public static class InGameFlowFactory
    {
        public static IInGameFlow CreateLocal(
            GameSession session)
            => new LocalInGameFlow(session);

        //public static IInGameFlow CreateNetwork(
        //    GameSession session,
        //    InGameHandler handler,
        //    InGameService service)
        //    => new NetworkInGameFlow(
        //        session,
        //        handler,
        //        service);
    }
}
