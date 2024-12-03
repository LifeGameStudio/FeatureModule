namespace GameModule.ScreenQueue.Scripts
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using UnityEngine;
    using Zenject;

    public class ScreenQueueService
    {
        private readonly ISignalBus    signalBus;
        private readonly ScreenManager screenManager;

        public ScreenQueueService(ISignalBus signalBus, ScreenManager screenManager)
        {
            this.signalBus     = signalBus;
            this.screenManager = screenManager;
        }

        private readonly Queue<Func<UniTask>> screenQueue           = new();
        public           PlayerLoopTiming     ScreenCloseCheckLogic = PlayerLoopTiming.Update;

        private bool automaticStartAfterAddScreenToQueue = true;

        public bool AutomaticStartAfterAddScreenToQueue
        {
            get => this.automaticStartAfterAddScreenToQueue;
            set
            {
                if (value && this.isQueueRunning)
                {
                    Debug.LogWarning("Automatic start only begin after the queue have been clear");
                }

                this.automaticStartAfterAddScreenToQueue = value;
            }
        }

        public void AddScreenToQueue<TScreenPresenter>() where TScreenPresenter : IScreenPresenter
        {
            Func<UniTask> openScreenAction = async () =>
            {
                var isScreenClosed  = false;
                var screenPresenter = await this.screenManager.OpenScreen<TScreenPresenter>();
                signalBus.Subscribe<ScreenCloseSignal>(OnScreenClose);

                void OnScreenClose(ScreenCloseSignal signal)
                {
                    if (signal.ScreenPresenter.ScreenId == screenPresenter.ScreenId)
                        isScreenClosed = true;
                }

                await UniTask.WaitUntil(() => isScreenClosed, ScreenCloseCheckLogic);
            };
            this.screenQueue.Enqueue(openScreenAction);

            if (this.AutomaticStartAfterAddScreenToQueue)
            {
                this.StartExecuteQueue();
            }
        }

        public void AddScreenToQueue<TScreenPresenter, TScreenModel>(TScreenModel model)
            where TScreenPresenter : IScreenPresenter<TScreenModel>
            where TScreenModel : class
        {
            Func<UniTask> openScreenAction = async () =>
            {
                var isScreenClosed  = false;
                var screenPresenter = await this.screenManager.OpenScreen<TScreenPresenter, TScreenModel>(model);
                signalBus.Subscribe<ScreenCloseSignal>(OnScreenClose);

                void OnScreenClose(ScreenCloseSignal signal)
                {
                    if (signal.ScreenPresenter.ScreenId == screenPresenter.ScreenId)
                        isScreenClosed = true;
                }

                await UniTask.WaitUntil(() => isScreenClosed, this.ScreenCloseCheckLogic);
            };

            this.screenQueue.Enqueue(openScreenAction);

            if (this.AutomaticStartAfterAddScreenToQueue)
            {
                this.StartExecuteQueue();
            }
        }

        private bool isQueueRunning = false;

        public async void StartExecuteQueue()
        {
            if (this.isQueueRunning)
                return;

            this.isQueueRunning = true;

            while (this.screenQueue.Count > 0)
            {
                var targetScreen = this.screenQueue.Peek();
                await targetScreen.Invoke();
                this.screenQueue.Dequeue();
            }

            this.isQueueRunning = false;
        }
    }
}