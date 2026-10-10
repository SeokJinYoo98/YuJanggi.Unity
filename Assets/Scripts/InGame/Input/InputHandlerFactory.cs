using System;
using UnityEngine;
using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Input
{
    public sealed class InputHandlerFactory
    {
        private readonly InputPrefabs _prefabs;
        private readonly Transform _parent;

        public InputHandlerFactory(
            InputPrefabs prefabs,
            Transform parent)
        {
            _prefabs = prefabs;
            _parent = parent;
        }

        public ILocalInputHandler Create(GameInputType type)
            => type switch
            {
                GameInputType.PC => UnityEngine.Object.Instantiate(
                    _prefabs.PointerInput, _parent),
                _ => throw new NotSupportedException(
                    $"아직 지원하지 않는 GameInput입니다: {type}")
            };
    }
}
