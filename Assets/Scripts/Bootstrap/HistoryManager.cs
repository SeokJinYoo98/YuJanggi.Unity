

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;
using YuJanggi.Engine.Domain;
using YuJanggi.InGame.Input;

namespace YuJanggi.BootStrap
{
    public sealed record HistoryMove
    {
        public Pos From { get; }
        public Pos To { get; }

        public HistoryMove(Pos from, Pos to)
        {
            From = from;
            To = to;
        }
    }

    public sealed class HistroyManager : MonoBehaviour
    {
        [Header("Record Mode")]
        [SerializeField]
        public History _history;
        [SerializeField, Range(0, 3)]
        private int _idx;

        private static readonly PlayerTeam[] Winners =
        {
            PlayerTeam.Han, // 01
            PlayerTeam.Cho, // 02
            PlayerTeam.Han, // 03
            PlayerTeam.Cho  // 04
        };

        public PlayerTeam Winner => Winners[_idx];

        private HistoryData _data;
        private readonly Queue<HistoryMove> _moves = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public Queue<HistoryMove> Moves => _moves;

        public Formation SelectTeam(PlayerTeam team)
        {
            if (_data == null)
                _data = JsonSerializer.Deserialize<HistoryData>(
                    _history.HistoryJsons[_idx].text, JsonOptions);

            _moves.Clear();
            foreach (var move in _data.Moves)
            {
                if (move.Team != team)
                    continue;

                _moves.Enqueue(new HistoryMove(
                    new Pos(move.From.X, move.From.Y),
                    new Pos(move.To.X, move.To.Y)));
            }

            var formation = team == PlayerTeam.Cho
                ? _data.ChoFormation
                : _data.HanFormation;

            return formation;
        }

        [Serializable]
        private sealed class HistoryData
        {
            public HistoryData() { }

            public Formation ChoFormation = default;
            public Formation HanFormation = default;
            public MoveData[] Moves = Array.Empty<MoveData>();
        }

        [Serializable]
        private sealed class MoveData
        {
            public MoveData() { }

            public PlayerTeam Team = default;
            public PositionData From = default;
            public PositionData To = default;
        }
        #pragma warning disable CS0649 // JSON 역직렬화로 값이 설정됩니다.
        [Serializable]
        private struct PositionData
        {
            public int X;
            public int Y;
        }
        #pragma warning restore CS0649
    }
}
