namespace GameModule.Tutorial.Scripts.TaskStateFlow.MonoUltilities
{
    using System;
    using UnityEngine;

    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class WorldClickable2D : MonoBehaviour, IClickable
    {
        public event Action Clicked;

        private void OnMouseDown()
        {
            this.Clicked?.Invoke();
        }
    }
}