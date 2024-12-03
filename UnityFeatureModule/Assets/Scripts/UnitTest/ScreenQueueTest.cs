namespace Game.Scripts.UnitTest
{
    using Cysharp.Threading.Tasks;
    using Game.Scripts.UnitTest.ScreenQueueTestView;
    using GameModule.ScreenQueue.Scripts;
    using GameModule.UnitTest;
    using Zenject;

    public class ScreenQueueTest : IUnitTest
    {
        [Inject] private ScreenQueueService screenQueueService;
        public async UniTask PreConditionAsync()
        {
            this.screenQueueService.AddScreenToQueue<PresenterA>();
            await UniTask.WaitForSeconds(1);
            this.screenQueueService.AddScreenToQueue<PresenterC>();
        }

        public async UniTask RunAsync()
        {
            await UniTask.WaitForSeconds(2);
        }

        public UniTask PostConditionAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}

/* Test case log:
 Predefine variable:
 View A: normal View without model.
 View B: normal View with model.
 View C: normal View without model.
 
Case 1: basic case
- Open A, B, C
- Close A, B, C

Output: Open 3 screen by queue

 Result: Success
 
 Case 1: case where 1 screen open delayed
- Open A, B
- Close A
- Open C
- Close B
- Close A

Output: Open 3 screen by queue

 Result: Success
 
Case 1: Open multiple popup in queue separately
- Open A
- Open A
- Close A
- Open A
-Close A
-Close A

Output: Open 3 screen by queue

 Result: Success
*/