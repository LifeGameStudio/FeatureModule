namespace Game.Scripts.UnitTest
{
    using System;
    using Cysharp.Threading.Tasks;
    using FeatureTemplate.Scripts.Services;
    using GameModule.TimeMarker.Scripts;
    using GameModule.UnitTest;
    using R3;

    public class TimeMarkTest : IUnitTest
    {
        private readonly TimeMarkService timeMarkService;
        
        

        public TimeMarkTest(TimeMarkService timeMarkService) { this.timeMarkService = timeMarkService; }

        public async UniTask PreConditionAsync()
        {
            this.timeMarkService.AddTimeMark("test", DateTime.Now);

            var timer = await this.timeMarkService.GetOrCreateTimeSpan("test");
            timer.Subscribe(this.ReadText);
        }

        public async UniTask RunAsync()
        {
            await UniTask.Delay(1000);
        }

        public UniTask PostConditionAsync()
        {
            return UniTask.CompletedTask;
        }

        private void ReadText(float text)
        {
            
            this.LogMessage(text);
        }
    }
}