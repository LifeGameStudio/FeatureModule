namespace GameModule.Tutorial.Scripts.TaskStateFlow.MonoUltilities
{
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [DisallowMultipleComponent]
    public class UIClickable : MonoBehaviour, IClickable
    {
        public event Action Clicked;

        private EventTrigger trigger;

        private void Awake()
        {
            this.trigger = this.gameObject.AddComponent<EventTrigger>();

            var entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };

            entry.callback.AddListener(_ => this.Clicked?.Invoke());
            this.trigger.triggers.Add(entry);
        }

        private void OnDestroy()
        {
            if (this.trigger != null)
                Destroy(this.trigger);
        }
    }
}