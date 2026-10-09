using System;
using System.Collections.Generic;
using UnityEngine;
using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Input
{
    [Serializable]
    public struct InputPrefabEntry
    {
        public GameInputType Type;
        public InputHandler Prefab;
    }

    [CreateAssetMenu(fileName = "InputPrefabs", menuName = "YuJanggi/Input/InputPrefabs")]
    public class InputPrefabs : ScriptableObject
    {
        [SerializeField] private List<InputPrefabEntry> _inputs = new();

        public IReadOnlyList<InputPrefabEntry> Inputs => _inputs;

        public InputHandler GetPrefab(GameInputType type)
        {
            InputHandler prefab = null;
            bool found = false;
            foreach (var entry in _inputs)
            {
                if (entry.Type != type)
                    continue;
                if (found)
                    throw new InvalidOperationException($"{type} 입력 Prefab이 중복 등록되었습니다.");

                found = true;
                prefab = entry.Prefab;
            }

            return prefab != null
                ? prefab
                : throw new InvalidOperationException($"{type} 입력 Prefab이 지정되지 않았습니다.");
        }
    }
}
