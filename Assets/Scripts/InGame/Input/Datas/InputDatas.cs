using System;
using System.Collections.Generic;
using UnityEngine;
using YuJanggi.Engine.Domain;

namespace YuJanggi.InGame.Input
{
    public enum GameInputType { PC, Record }

    [Serializable]
    public struct InputPrefabEntry
    {
        public GameInputType Type;
        public InputHandler Prefab;
    }
    [CreateAssetMenu(fileName = "InputPrefabs", menuName = "YuJanggi/InputData")]
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
    [Serializable]
    public struct HistoryData
    {
        public TextAsset HistoryJson;
        public Formation ChoFormation;
        public Formation HanFormation;
    }

    [CreateAssetMenu(fileName = "History", menuName = "YuJanggi/InputData")]
    public sealed class History : ScriptableObject
    {
        [SerializeField]
        private HistoryData _data = new()
        {
            ChoFormation = Formation.HEEH,
            HanFormation = Formation.HEHE
        };

        public HistoryData Data => _data;
    }
}

