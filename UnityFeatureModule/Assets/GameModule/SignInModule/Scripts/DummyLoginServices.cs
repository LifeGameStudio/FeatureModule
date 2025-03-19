namespace GameModule.SignInModule.Scripts
{
    using Cysharp.Threading.Tasks;

    public class DummyLoginServices : ILoginServices
    {
        public async UniTask<(string, string)> SignIn()
        {
            var result = ("", "");

            return result;
        }

        public void ClearSession() { }
    }
}