namespace GameModule.SaveLoadGameCloud.Scripts.Interfaces
{
    using Cysharp.Threading.Tasks;

    public interface IHandleDataCloud
    {
        UniTask Login();
    }
}