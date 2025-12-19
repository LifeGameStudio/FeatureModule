namespace GameModule.Tutorial.Scripts.TaskStateFlow.MonoUltilities
{
    using System;
    using UnityEngine;

    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class WorldClickable3D : MonoBehaviour, IClickable
    {
        public event Action Clicked;

        private void OnMouseDown()
        {
            this.Clicked?.Invoke();
        }
    }
}