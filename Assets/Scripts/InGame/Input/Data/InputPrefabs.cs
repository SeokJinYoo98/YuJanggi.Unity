using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Input
{
    [CreateAssetMenu(fileName = "InputPrefabs", menuName = "YuJanggi/Input/InputPrefabs")]
    public class InputPrefabs : ScriptableObject
    {
        public PointerInputHandler PointerInput;
    }
}
