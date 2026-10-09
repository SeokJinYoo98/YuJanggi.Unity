using System.Collections.Generic;
using UnityEngine;


namespace YuJanggi.InGame.Input
{
    [CreateAssetMenu(fileName = "History", menuName = "YuJanggi/Input/History")]
    public sealed class History : ScriptableObject
    {
        [SerializeField] private List<TextAsset> _historyJsons = new();

        public IReadOnlyList<TextAsset> HistoryJsons => _historyJsons;
    }
}
