

using Cysharp.Threading.Tasks;

namespace YuJanggi.Core.Panel
{
    public interface IGameStartPanel
    {
        UniTask<bool> PrepareGameAsync();
    }

}
