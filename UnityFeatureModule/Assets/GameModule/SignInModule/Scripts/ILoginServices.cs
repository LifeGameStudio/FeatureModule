namespace GameModule.SignInModule.Scripts
{
    using Cysharp.Threading.Tasks;

    public interface ILoginServices
    {
        UniTask<(string,string)> SignIn();
        void                     ClearSession();
    }
}