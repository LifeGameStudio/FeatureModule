namespace GameModule.SignInModule.Scripts
{
    using Assets.SimpleGoogleSignIn.Scripts;
    using Cysharp.Threading.Tasks;

    public class GoogleLoginServices : ILoginServices
    {
        public async UniTask<(string, string)> SignIn()
        {
            var result     = ("", "");
            var isComplete = false;

            GoogleAuth.GetAccessToken((b, s, arg3) =>
            {
                if (b)
                {
                    result = (arg3.IdToken, arg3.RefreshToken);
                }
                isComplete = true;

            });

            await UniTask.WaitUntil(() => isComplete);

            return result;
        }

        public void ClearSession() { SavedAuth.Instance.Delete(); }
    }
}