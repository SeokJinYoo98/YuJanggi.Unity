using System.Collections;
using UnityEngine;

namespace YuJanggi.Runtime.Input
{
    public interface ICoroutineRunner
    {
        Coroutine Run(IEnumerator routine);
        void Stop(Coroutine routine);
    }
    public class CoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        public Coroutine Run(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }
        public void Stop(Coroutine routine)
        {
            StopCoroutine(routine);
        }
    }

}
