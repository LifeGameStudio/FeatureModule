namespace GameModule.UnitTest
{
    using Cysharp.Threading.Tasks;

    public interface IUnitTest
    {
        UniTask PreConditionAsync();
        UniTask RunAsync();
        UniTask PostConditionAsync();
    }
}