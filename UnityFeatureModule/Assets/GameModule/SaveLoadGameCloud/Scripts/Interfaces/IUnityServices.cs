#if UNITY_CLOUD

namespace GameModule.SaveLoadGameCloud.Scripts.Interfaces
{
    using System;
    using Cysharp.Threading.Tasks;
    using Unity.Services.Authentication;
    using Unity.Services.Core;

    public interface IUnityServices
    {
        UniTask LinkGoogleAccount(bool isForce, Action<AuthenticationException> authenticationException = null, Action<RequestFailedException> requestFailedException = null,
            Action onComplete = null);
    }
}
#endif