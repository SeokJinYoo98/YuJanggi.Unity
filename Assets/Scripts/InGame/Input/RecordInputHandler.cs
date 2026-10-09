using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;
using YuJanggi.Core.InGame;
namespace YuJanggi.InGame.Input
{
    using Engine.Domain;
    using Engine.JanggiEngine;
    using Engine.JanggiOption;
    using Store;


    /// <summary>클릭 입력을 거치지 않고 기보의 이동을 기존 이동 요청으로 전달합니다.</summary>
    public sealed class RecordInputHandler : InputHandler
    {
        [SerializeField] private History _history;
        [SerializeField, Min(0)] private int _historyIndex;
        [SerializeField, Min(0f)] private float _moveInterval = 1f;

        private readonly List<RecordMove> _moves = new();
        private IGameInputReceiver _receiver;
        private IJanggiEngine _engine;
        private float _elapsed;
        private bool _requestPending;
        private bool _moveConfirmed;

        public int NextMoveIndex { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsCompleted => _engine != null && NextMoveIndex == _moves.Count;
        public string Error { get; private set; } = string.Empty;

        public override bool Initialize(Camera inputCamera = null)
        {
            Release();
            _moves.Clear();
            NextMoveIndex = 0;
            Error = string.Empty;
            _elapsed = 0f;

            try
            {
                if (_history == null)
                    throw new InvalidDataException("History SO가 지정되지 않았습니다.");

                var histories = _history.HistoryJsons;
                if (_historyIndex < 0 || _historyIndex >= histories.Count)
                    throw new InvalidDataException($"기보 인덱스가 목록 범위를 벗어났습니다: {_historyIndex}");

                var historyJson = histories[_historyIndex];
                if (historyJson == null)
                    throw new InvalidDataException($"{_historyIndex}번 기보 JSON이 지정되지 않았습니다.");

                LoadHistory(historyJson.text);
                return true;
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
                Release();
                return false;
            }
        }

        public override void Bind(IGameInputReceiver receiver, IJanggiEngine engine)
        {
            if (receiver == null)
                throw new ArgumentNullException(nameof(receiver));
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            Release();
            _receiver = receiver;
            _engine = engine;
            _engine.GameEvents.OnPieceMoved += HandlePieceMoved;
        }

        public override void Activate()
        {
            if (_engine == null || !string.IsNullOrEmpty(Error) || IsCompleted)
                return;

            _elapsed = 0f;
            IsPlaying = true;
        }

        public override void Deactivate() => IsPlaying = false;

        public override void Release()
        {
            Deactivate();
            if (_engine != null)
                _engine.GameEvents.OnPieceMoved -= HandlePieceMoved;
            _engine = null;
            _receiver = null;
            _requestPending = false;
        }

        private void Update()
        {
            if (!IsPlaying || _requestPending)
                return;

            _elapsed += Time.deltaTime;
            if (_elapsed < _moveInterval)
                return;
            _elapsed = 0f;

            try
            {
                ApplyNextMove();
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
            }
        }

        private void ApplyNextMove()
        {
            if (NextMoveIndex >= _moves.Count)
            {
                Deactivate();
                return;
            }

            var move = _moves[NextMoveIndex];
            if (_engine.CurrentTurn != move.Team)
                throw new InvalidDataException(
                    $"턴 불일치: 기보 {move.Team}, 현재 {_engine.CurrentTurn}.");
            if (!_engine.IsValidPiece(move.Team, move.From, out _))
                throw new InvalidDataException("출발 좌표에 해당 진영 기물이 없습니다.");

            _moveConfirmed = false;
            _requestPending = true;
            try
            {
                // 현재 GameMode → Engine 이동 적용과 OnPieceMoved는 동기적으로 완료됩니다.
                _receiver.RequestMove(move.From, move.To);
                if (!_moveConfirmed)
                    throw new InvalidDataException("이동이 거부되었거나 적용 완료 이벤트가 없습니다.");

                ++NextMoveIndex;
                if (IsCompleted)
                {
                    Deactivate();
                    Debug.Log($"[RecordInputHandler] 기보 {_moves.Count}수 재생 완료.", this);
                }
            }
            finally
            {
                _requestPending = false;
            }
        }

        private void HandlePieceMoved(MoveContext context)
        {
            if (!_requestPending || context.IsHandicap)
                return;

            var move = _moves[NextMoveIndex];
            var record = context.Record;
            _moveConfirmed = record.From == move.From
                && record.To == move.To
                && record.MovedPiece.Team == move.Team;
        }

        private void LoadHistory(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("기보 JSON은 ChoFormation, HanFormation, Moves를 포함한 객체여야 합니다.");

            var choFormation = ReadFormation(root, "ChoFormation");
            var hanFormation = ReadFormation(root, "HanFormation");
            if (!root.TryGetProperty("Moves", out var moves)
                || moves.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("기보 JSON의 Moves는 배열이어야 합니다.");

            foreach (var item in moves.EnumerateArray())
            {
                int number = _moves.Count + 1;
                if (item.ValueKind != JsonValueKind.Object
                    || !item.TryGetProperty("Team", out var teamElement)
                    || teamElement.ValueKind != JsonValueKind.String)
                    throw new InvalidDataException($"{number}수: Team 문자열이 필요합니다.");

                var team = teamElement.GetString() switch
                {
                    "Cho" => PlayerTeam.Cho,
                    "Han" => PlayerTeam.Han,
                    _ => throw new InvalidDataException($"{number}수: Team은 Cho 또는 Han이어야 합니다.")
                };
                _moves.Add(new RecordMove(team,
                    ReadPosition(item, "From", number),
                    ReadPosition(item, "To", number)));
            }

            JanggiOptionStore.SaveOptions(new JanggiOptions
            {
                GameMode        = GameModeType.Local,
                PlayerCho       = PlayerType.Local,
                PlayerHan       = PlayerType.Local,
                ChoFormation    = choFormation,
                HanFormation    = hanFormation,
                TurnTime        = 0f
            });
        }

        private static Formation ReadFormation(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var value)
                || value.ValueKind != JsonValueKind.String)
                throw new InvalidDataException($"기보 JSON의 {name}은 포진 이름 문자열이어야 합니다.");

            return value.GetString() switch
            {
                "HEHE" => Formation.HEHE,
                "EHEH" => Formation.EHEH,
                "EHHE" => Formation.EHHE,
                "HEEH" => Formation.HEEH,
                _ => throw new InvalidDataException($"유효하지 않은 {name}: {value.GetString()}")
            };
        }

        private static Pos ReadPosition(JsonElement item, string name, int number)
        {
            if (!item.TryGetProperty(name, out var position)
                || position.ValueKind != JsonValueKind.Object
                || !position.TryGetProperty("X", out var xElement)
                || !position.TryGetProperty("Y", out var yElement)
                || xElement.ValueKind != JsonValueKind.Number
                || yElement.ValueKind != JsonValueKind.Number
                || !xElement.TryGetInt32(out int x)
                || !yElement.TryGetInt32(out int y)
                || x < 0 || x > 8 || y < 0 || y > 9)
                throw new InvalidDataException($"{number}수: {name} 좌표는 정수 X=0~8, Y=0~9여야 합니다.");

            return new Pos(x, y);
        }

        private void Fail(string reason)
        {
            Deactivate();
            Error = reason;
            Debug.LogError($"[RecordInputHandler] {NextMoveIndex + 1}수에서 중단: {reason}", this);
        }

        private void OnDisable() => Deactivate();

        private readonly struct RecordMove
        {
            public RecordMove(PlayerTeam team, Pos from, Pos to)
            {
                Team = team;
                From = from;
                To = to;
            }

            public PlayerTeam Team { get; }
            public Pos From { get; }
            public Pos To { get; }
        }
    }
}
