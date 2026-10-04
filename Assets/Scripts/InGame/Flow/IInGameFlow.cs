

using Cysharp.Threading.Tasks;
using System.Threading;
using YuJanggi.InGame.Session;
using YuJanggi.InGame.Controller;
using YuJanggi.Engine.Domain;
using System.Collections.Generic;

namespace YuJanggi.InGame.Flow
{
    public interface IInGameFlow : IGameInputReceiver
    {
        UniTask EnterAsync(CancellationToken cancellationToken);
        void Exit();
        void PrepareReturnToLobby();
    }
    internal abstract class InGameFlow : IInGameFlow
    {
        #region Fields
        private bool                _entered;
        private CancellationToken   _entryToken;

        protected GameSession       Session { get; }
        #endregion

        #region Constructor
        protected InGameFlow(GameSession session)
        {
            Session = session;
        }
        #endregion

        #region Public Methods
        public async UniTask EnterAsync(
            CancellationToken cancellationToken)
        {
            if (_entered)
                return;

            cancellationToken.ThrowIfCancellationRequested();

            _entered = true;
            _entryToken = cancellationToken;

            try
            {
                Bind();
                await StartAsync(cancellationToken);
            }
            catch
            {
                if (_entered && _entryToken == cancellationToken)
                    Exit();
                throw;
            }
        }

        public void SelectPiece(
            int? pieceId,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal)
            => Session.ChangeSelection(pieceId, legal, illegal);
        public abstract void RequestMove(
            Pos from, Pos to);
        public virtual void SubmitGameResult(in GameResultInfo info) { }
        public virtual void PrepareReturnToLobby() { }
        public void Exit()
        {
            if (!_entered)
                return;

            _entered = false;
            _entryToken = default;
            UnBind();
        }
        #endregion

        protected virtual void Bind()
        {
        }
        protected virtual void UnBind()
        {
        }

        protected abstract UniTask StartAsync(
            CancellationToken cancellationToken);
    }
}


