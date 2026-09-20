using UnityEngine;

namespace YuJanggi.Runtime.Board
{ 
    using Core.V2.Domain;
    using System.Collections.Generic;
    using UnityEngine.Pool;

    public class MoveGuideView : MonoBehaviour
    {
        [SerializeField] private MoveGuideCellView _prefab;
        private ObjectPool<MoveGuideCellView>      _pool;
        private List<MoveGuideCellView> _active;
        void Awake()
        {
            _active = new List<MoveGuideCellView>(25);

            _pool = new ObjectPool<MoveGuideCellView>(
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
