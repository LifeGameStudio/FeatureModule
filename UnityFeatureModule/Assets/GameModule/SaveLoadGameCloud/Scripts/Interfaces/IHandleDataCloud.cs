namespace GameModule.SaveLoadGameCloud.Scripts.Interfaces
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;

    public interface IHandleDataCloud
    {
        UniTask                Login();
        UniTask<Dictionary<string,string>> LoadData(bool forceOverrideToLocal = false);
        UniTask                Logout();
        
        bool IsSignedIn { get; }
    }
}