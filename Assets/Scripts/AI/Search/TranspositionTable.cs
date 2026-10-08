using System.Collections.Generic;

namespace YuJanggi.AI.Search
{
    using Data;
    using Engine.Domain;
    using Engine.JanggiEngine;

    internal sealed class TranspositionTable
    {
        private readonly Dictionary<ulong, Entry> _entries = new();

        public bool TryGet(IAIPosition position, PlayerTeam team, int depth, out int score)
        {
            if (_entries.TryGetValue(CalculatePositionKey(position, team), out var entry) && entry.Depth >= depth)
            {
                score = entry.Score;
                return true;
            }
            score = default;
            return false;
        }

        public void Store(IAIPosition position, PlayerTeam team, int depth, int score)
            => _entries[CalculatePositionKey(position, team)] = new Entry(depth, score);

        private static ulong CalculatePositionKey(IAIPosition position, PlayerTeam currentTeam)
        {
            const ulong offsetBasis = 14_695_981_039_346_656_037UL;
            const ulong prime = 1_099_511_628_211UL;
            ulong hash = offsetBasis;
            for (int x = 0; x < position.Width; ++x)
            {
                for (int z = 0; z < position.Height; ++z)
                {
                    var piece = position.GetPiece(new Pos(x, z));
                    hash ^= (ulong)((int)piece.Team + 1);
                    hash *= prime;
                    hash ^= (ulong)((int)piece.Type + 1);
                    hash *= prime;
                }
            }
            hash ^= (ulong)((int)currentTeam + 1);
            return hash;
        }

    }
}
