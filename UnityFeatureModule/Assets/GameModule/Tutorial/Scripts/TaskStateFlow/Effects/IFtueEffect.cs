namespace GameModule.Tutorial.Scripts.TaskStateFlow.Effects
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public interface IFtueEffect
    {
        UniTask Initialize(GameObject targetObject);

        void Cleanup();
    }
}