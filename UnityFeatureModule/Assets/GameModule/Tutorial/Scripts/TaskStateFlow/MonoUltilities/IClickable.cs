namespace GameModule.Tutorial.Scripts.TaskStateFlow.MonoUltilities
{
    using System;

    public interface IClickable
    {
        event Action Clicked;
    }
}