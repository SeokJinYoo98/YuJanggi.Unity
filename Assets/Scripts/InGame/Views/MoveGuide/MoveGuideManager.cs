using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace YuJanggi.InGame.Views.Board
{ 
    using Engine.Domain;


    public class MoveGuideManager : MonoBehaviour
    {
        [SerializeField] private MoveGuideView _prefab;
        private ObjectPool<MoveGuideView>      _pool;
        private List<MoveGuideView>            _active;
        void Awake()
        {
            _active = new List<MoveGuideView>(25);

            _pool = new ObjectPool<MoveGuideView>(
                ()  => Instantiate(_prefab, transform),
                obj => obj.Show(true),
                obj => obj.Hide(),
                obj => Destroy(obj.gameObject),
                false,
                25,
                25
            );

            for (int i = 0; i < 25; i++)
                _pool.Release(_pool.Get());
        }
        public void ShowHighlight(IReadOnlyList<Pos> cells, bool isLegal)
        {
            int length = cells.Count;
            for (int i = 0; i < length; ++i)
            {
                var pos = cells[i];
                var highlight = _pool.Get();
                highlight.Show(isLegal);
                highlight.MoveTo(pos);
                _active.Add(highlight);
            }
        }
        public void HideHighlight()
        {
            foreach (var h in _active)
                _pool.Release(h);

            _active.Clear();
        }

    }
}


