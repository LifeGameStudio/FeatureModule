namespace Game.Scripts.UnitTest
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameModule.UnitTest;
    using UnityEngine;
    using Zenject;

    // Define a sample signal
    public class NewSignal
    {
        public string Message { get; set; }
    }

    public class ObserverTest : IUnitTest
    {
        private          ISignalBus   _observer;
        private readonly ILogService  logService;
        private          List<string> _log;

        public ObserverTest(ISignalBus signalBus, ILogService logService)
        {
            // _observer = new Observer();
            _observer       = signalBus;
            this.logService = logService;
            _log            = new List<string>();
        }

        public async UniTask PreConditionAsync()
        {
            // Add subscribers
            _observer.Subscribe<NewSignal>(signal => _log.Add($"Received: {signal.Message}"));
            _observer.Subscribe<NewSignal>(() => _log.Add("Received signal with no payload"));
            await UniTask.CompletedTask;
        }

        public async UniTask RunAsync() { await this.RunAllTestsAsync(); }

        public UniTask PostConditionAsync()
        {
            // Unsubscribe from all NewSignal subscriptions
            _observer.Unsubscribe<NewSignal>(signal => _log.Add($"Received: {signal.Message}"));
            _observer.Unsubscribe<NewSignal>(() => _log.Add("Received signal with no payload"));

            foreach (var item in this._log)
            {
                this.logService.LogWithColor(item, Color.cyan);
            }

            this.logService.LogWithColor("Unsubscribed all signals in PostConditionAsync.", Color.cyan);

            return UniTask.CompletedTask;
        }

        private async UniTask RunBasicCaseAsync()
        {
            this.logService.LogWithColor("Running Basic Case...", Color.cyan);

            // Fire signals
            _observer.Fire(new NewSignal { Message = "Test message 1" });
            _observer.Fire<NewSignal>();

            // Simulate delay
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            // Verify logs
            Assert(_log.Count == 2, "Basic case failed: Expected 2 logs.");
            Assert(_log[0] == "Received: Test message 1", "Basic case failed: Incorrect message in log[0].");
            Assert(_log[1] == "Received signal with no payload", "Basic case failed: Incorrect message in log[1].");

            this.logService.LogWithColor("Basic Case Passed.", Color.cyan);
        }

        private async UniTask RunDelayedOpenCaseAsync()
        {
            this.logService.LogWithColor("Running Delayed Open Case...", Color.cyan);

            // Clear log
            _log.Clear();

            // Fire first signal
            _observer.Fire(new NewSignal { Message = "First signal" });

            // Simulate delay and fire another signal
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            _observer.Fire(new NewSignal { Message = "Second signal" });

            // Verify logs
            Assert(this._log.Count == 2, "Delayed open case failed: Expected 2 logs.");
            Assert(this._log[0] == "Received: First signal", "Delayed open case failed: Incorrect message in log[0].");
            Assert(this._log[1] == "Received: Second signal", "Delayed open case failed: Incorrect message in log[1].");

            this.logService.LogWithColor("Delayed Open Case Passed.", Color.cyan);
        }

        private async UniTask RunMultipleOpenCloseAsync()
        {
            this.logService.LogWithColor("Running Multiple Open-Close Case...", Color.cyan);

            // Clear log
            this._log.Clear();

            // Subscribe and fire signals multiple times
            this._observer.Fire(new NewSignal { Message = "First open" });
            this._observer.Fire(new NewSignal { Message = "Second open" });
            this._observer.Fire<NewSignal>();

            // Simulate delay
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            // Verify logs
            this.Assert(this._log.Count == 3, "Multiple open-close case failed: Expected 3 logs.");
            this.Assert(this._log[0] == "Received: First open", "Multiple open-close case failed: Incorrect message in log[0].");
            this.Assert(this._log[1] == "Received: Second open", "Multiple open-close case failed: Incorrect message in log[1].");
            this.Assert(this._log[2] == "Received signal with no payload", "Multiple open-close case failed: Incorrect message in log[2].");

            this.logService.LogWithColor("Multiple Open-Close Case Passed.", Color.cyan);
        }

        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Test Failed: {message}");
            }
        }

        private async UniTask RunAllTestsAsync()
        {
            this.logService.LogWithColor("Starting Observer Tests...", Color.cyan);
            await this.RunBasicCaseAsync();
            await this.RunDelayedOpenCaseAsync();
            await this.RunMultipleOpenCloseAsync();
            this.logService.LogWithColor("All Tests Passed!", Color.cyan);
        }
    }
}