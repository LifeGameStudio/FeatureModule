using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.SimpleGoogleSignIn.Scripts
{
    public static class Extensions
    {
        public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
        {
            var tcs = new TaskCompletionSource<AsyncOperation>();

            asyncOp.completed += operation => { tcs.SetResult(operation); };

            return ((Task) tcs.Task).GetAwaiter();
        }
    }
}