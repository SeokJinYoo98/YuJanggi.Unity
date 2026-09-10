using System;
using System.Collections.Generic;
using YuJanggiCommon;

namespace Yujanggi.Runtime.Network
{
    public enum OnlineMatchPhase
    {
        SelectingFormation,
        WaitingForGameStart,
        GameStarted
    }

    /// <summary>
    /// Holds the server-authoritative identity and latest start state of one online match.
    /// The context survives scene changes through OnlineGameClient.CurrentMatch.
    /// </summary>
    public sealed class OnlineMatchContext
    {
        private IReadOnlyList<BoardPieceState> _pieces = Array.Empty<BoardPieceState>();

        public OnlineMatchContext(MatchFoundResponse match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            if (match.GameId == Guid.Empty)
                throw new ArgumentException("온라인 대국 ID가 필요합니다.", nameof(match));
            if (match.Opponent == null)
                throw new ArgumentException("상대 플레이어 정보가 필요합니다.", nameof(match));

            GameId = match.GameId;
            Opponent = match.Opponent;
            LocalSide = match.Side;
            MatchMessage = match.Message ?? string.Empty;
            RequiresFormationSelection = match.RequiresFormationSelection;
            Phase = RequiresFormationSelection
                ? OnlineMatchPhase.SelectingFormation
                : OnlineMatchPhase.WaitingForGameStart;
        }

        public Guid GameId { get; }
        public MatchedPlayer Opponent { get; }
        public PlayerSide LocalSide { get; }
        public string MatchMessage { get; }
        public bool RequiresFormationSelection { get; }
        public GameFormation? SelectedFormation { get; private set; }
        public PlayerSide? CurrentTurn { get; private set; }
        public GameFormation? ChoFormation { get; private set; }
        public GameFormation? HanFormation { get; private set; }
        public IReadOnlyList<BoardPieceState> Pieces => _pieces;
        public OnlineMatchPhase Phase { get; private set; }
        public bool HasStarted => Phase == OnlineMatchPhase.GameStarted;

        public void ApplyFormationSelected(FormationSelectedResponse response)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            EnsureSameGame(response.GameId);
            SelectedFormation = response.Formation;
            Phase = OnlineMatchPhase.WaitingForGameStart;
        }

        public void ApplyGameStart(GameStartEvent gameStart)
        {
            if (gameStart == null)
                throw new ArgumentNullException(nameof(gameStart));

            EnsureSameGame(gameStart.GameId);
            if (gameStart.Side != LocalSide)
                throw new InvalidOperationException("매칭 결과와 게임 시작 진영이 일치하지 않습니다.");
            if (gameStart.Pieces == null)
                throw new InvalidOperationException("게임 시작 기물 정보가 없습니다.");

            CurrentTurn = gameStart.CurrentTurn;
            ChoFormation = gameStart.ChoFormation;
            HanFormation = gameStart.HanFormation;
            _pieces = new List<BoardPieceState>(gameStart.Pieces).AsReadOnly();
            Phase = OnlineMatchPhase.GameStarted;
        }

        private void EnsureSameGame(Guid gameId)
        {
            if (gameId != GameId)
                throw new InvalidOperationException("현재 매칭과 다른 대국 메시지입니다.");
        }
    }
}
