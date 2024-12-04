namespace Game.Scripts.UnitTest
{
    using Cysharp.Threading.Tasks;
    using GameModule.UnitTest;

    public class StateMachineUnitTest : IUnitTest
    {
        public UniTask PreConditionAsync() { return UniTask.CompletedTask; }

        public UniTask RunAsync() { return UniTask.CompletedTask; }

        public UniTask PostConditionAsync() { return UniTask.CompletedTask; }
    }
}

/* Test case log:
 Predefine variable:
 HomeState : Enter from start, Automatic change to Playing state
 PlayingState : Change to End state when press 'X'
 EndState : Final state

Case 1: 
Invoke the state machine then press 'X'
Check the log

Output: 
Enter state: ExampleHomeState
Exit state: ExampleHomeState
Enter state: ExamplePlayingState
Exit state: ExamplePlayingState
Enter state: ExampleEndState
Exit state: ExampleEndState

 Result: Success

 Result: Success
*/