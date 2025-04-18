namespace GameModule.SaveLoadGameCloud.Scripts.Interfaces
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;

    public interface IHandleDataCloud
    {
        UniTask                            Login();
        UniTask<Dictionary<string,string>> LoadData();
        UniTask                            SaveDataFromCloudToLocal(Dictionary<string,string> input);
        UniTask                            Logout();
        
        bool   IsSignedIn { get; }
        string UserId     { get; }
    }
}