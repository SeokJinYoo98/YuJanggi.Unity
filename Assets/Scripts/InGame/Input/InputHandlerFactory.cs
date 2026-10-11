using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YuJanggi.BootStrap;
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
                GameInputType.Pointer => UnityEngine.Object.Instantiate(
                    _prefabs.PointerInput, _parent),

                GameInputType.Record => new RecordInputHandler(
                    YuJanggiBootStrap.Instance.HistoryManger,
                    _parent.gameObject.GetCancellationTokenOnDestroy()),

                _ => throw new NotSupportedException(
                    $"아직 지원하지 않는 GameInput입니다: {type}")
            };
    }
}
