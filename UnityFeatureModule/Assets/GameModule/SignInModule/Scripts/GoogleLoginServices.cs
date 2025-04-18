namespace GameModule.SignInModule.Scripts
{
    using System;
    using System.Threading;
    using Assets.SimpleGoogleSignIn.Scripts;
    using Cysharp.Threading.Tasks;

    public class GoogleLoginServices : ILoginServices
    {
        private CancellationTokenSource signInCts;

        public async UniTask<(string, string)> SignIn()
        {
            this.signInCts?.Cancel();
            this.signInCts = new CancellationTokenSource();

            var result     = ("", "");
            var isComplete = false;

            var token = this.signInCts.Token;

            GoogleAuth.GetAccessToken((b, s, arg3) =>
            {
                if (token.IsCancellationRequested) return;

                if (b)
                {
                    result = (arg3.IdToken, arg3.RefreshToken);
                }

                isComplete = true;
            });

            try
            {
                await UniTask.WaitUntil(() => isComplete, cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return ("", "");
            }

            return result;
        }

        public void ClearSession() { SavedAuth.Instance?.Delete(); }
        public bool IsSignedIn     => SavedAuth.Instance.UserInfo != null;
    }
}