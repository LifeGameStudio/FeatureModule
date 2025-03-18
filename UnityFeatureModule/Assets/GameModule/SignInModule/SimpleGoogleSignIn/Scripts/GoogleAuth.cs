using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.SimpleGoogleSignIn.Scripts
{
    /// <summary>
    /// API specification: https://developers.google.com/identity/protocols/oauth2/native-app
    /// </summary>
    public static class GoogleAuth
    {
        public static TokenResponse TokenResponse { get; private set; }
        public static SavedAuth SavedAuth => SavedAuth.Instance;

        /// <summary>
        /// OpenID configuration: https://accounts.google.com/.well-known/openid-configuration
        /// </summary>
        private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";
        private const string RevocationEndpoint = "https://oauth2.googleapis.com/revoke";

        private static Implementation _implementation;
        private static string _redirectUri, _state, _codeVerifier;
        private static Action<bool, string, TokenResponse> _callbackT;
        private static Action<bool, string, UserInfo> _callbackU;

        public static void SignIn(Action<bool, string, UserInfo> callback, bool caching = true)
        {
            _callbackU = callback;
            _callbackT = null;

            Initialize();

            if (SavedAuth == null)
            {
                Auth();
            }
            else if (caching && SavedAuth.UserInfo != null)
            {
                callback(true, null, SavedAuth.UserInfo);
            }
            else
            {
                UseSavedToken();
            }
        }

        public static void GetAccessToken(Action<bool, string, TokenResponse> callback)
        {
            _callbackT = callback;
            _callbackU = null;

            Initialize();

            if (SavedAuth == null)
            {
                Auth();
            }
            else
            {
                if (SavedAuth.TokenResponse.Expired)
                {
                    Debug.Log("Refreshing expired access token...");
                    RefreshAccessToken(callback);
                }
                else
                {
                    callback(true, null, SavedAuth.TokenResponse);
                }
            }
        }

        public static void SignOut(bool revokeAccessToken = false)
        {
            TokenResponse = null;

            if (SavedAuth != null)
            {
                if (revokeAccessToken && SavedAuth.TokenResponse != null)
                {
                    RevokeAccessToken(SavedAuth.TokenResponse.AccessToken);
                }

                SavedAuth.Delete();
            }
        }

        private static void Initialize()
        {
            #if UNITY_EDITOR

            _implementation = Implementation.LoopbackFlow;
            _redirectUri = $"http://localhost:{Utils.GetRandomUnusedPort()}/";
            
            #elif UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_WSA

            _implementation = Implementation.DeepLinking;
            _redirectUri = $"{Settings.Instance.CustomUriScheme}:/oauth2/google";

            #elif UNITY_STANDALONE_WIN

            _implementation = Settings.Instance.CustomUriScheme == "" ? Implementation.LoopbackFlow : Implementation.DeepLinking;
            _redirectUri = Settings.Instance.CustomUriScheme == "" ? $"http://localhost:{Utils.GetRandomUnusedPort()}/" : $"{Settings.Instance.CustomUriScheme}:/oauth2/google";

            #elif UNITY_WEBGL

            _implementation = Implementation.AuthorizationMiddleware;
            _redirectUri = "";

            #endif

            if (SavedAuth != null && SavedAuth.ClientId != Settings.Instance.ClientId)
            {
                SavedAuth.Instance.Delete();
            }
        }

        private static void Auth()
        {
            _state = Guid.NewGuid().ToString();
            _codeVerifier = Guid.NewGuid().ToString();

            var codeChallenge = Utils.CreateCodeChallenge(_codeVerifier);
            var redirectUri = _implementation == Implementation.AuthorizationMiddleware ? AuthorizationMiddleware.Endpoint + "/redirect" : _redirectUri;
            var authorizationRequest = $"{AuthorizationEndpoint}?response_type=code&scope={Uri.EscapeDataString(string.Join(" ", Settings.Instance.AccessScopes))}&redirect_uri={Uri.EscapeDataString(redirectUri)}&client_id={Settings.Instance.ClientId}&state={_state}&code_challenge={codeChallenge}&code_challenge_method=S256";

            if (_implementation == Implementation.AuthorizationMiddleware)
            {
                AuthorizationMiddleware.Auth(authorizationRequest, _redirectUri, _state, (success, error, code) =>
                {
                    if (success)
                    {
                        PerformCodeExchange(code);
                    }
                    else
                    {
                        _callbackU(false, error, null);
                    }
                });
            }
            else
            {
                Log($"Authorization: {authorizationRequest}");

                #if UNITY_IOS && !UNITY_EDITOR

                if (Settings.Instance.UseSafariViewController) SafariViewController.OpenURL(authorizationRequest);
                else Application.OpenURL(authorizationRequest);

                #else

                Application.OpenURL(authorizationRequest);

                #endif

                switch (_implementation)
                {
                    case Implementation.LoopbackFlow:
                        LoopbackFlow.Initialize(_redirectUri, OnDeepLinkActivated);
                        break;
                    case Implementation.DeepLinking:
                        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                        WindowsDeepLinking.Initialize(Settings.Instance.CustomUriScheme, OnDeepLinkActivated);
                        #endif
                        break;
                }
            }
        }

        private static void UseSavedToken()
        {
            if (SavedAuth == null || SavedAuth.ClientId != Settings.Instance.ClientId)
            {
                SignOut();
                SignIn(_callbackU);
            }
            else if (!SavedAuth.TokenResponse.Expired)
            {
                Debug.Log("Using saved access token...");
                RequestUserInfo(SavedAuth.TokenResponse.AccessToken, (success, error, userInfo) =>
                {
                    if (success)
                    {
                        _callbackU(true, null, userInfo);
                    }
                    else
                    {
                        SignOut();
                        SignIn(_callbackU);
                    }
                });
            }
            else
            {
                Debug.Log("Refreshing expired access token...");
                RefreshAccessToken((success, error, tokenResponse) =>
                {
                    if (success)
                    {
                        RequestUserInfo(SavedAuth.TokenResponse.AccessToken, _callbackU);
                    }
                    else
                    {
                        SignOut();
                        SignIn(_callbackU);
                    }
                });
            }
        }

        static GoogleAuth()
        {
            Application.deepLinkActivated += OnDeepLinkActivated;
        }

        private static void OnDeepLinkActivated(string deepLink)
        {
            Log($"Deep link activated: {deepLink}");

            if (_redirectUri == null || !deepLink.StartsWith(_redirectUri)) return;

            #if UNITY_IOS && !UNITY_EDITOR

            if (Settings.Instance.UseSafariViewController) SafariViewController.Close();

            #endif

            var parameters = Utils.ParseQueryString(deepLink);
            var error = parameters.Get("error");

            if (error != null)
            {
                _callbackU?.Invoke(false, error, null);
                return;
            }

            var state = parameters.Get("state");
            var code = parameters.Get("code");
            
            if (state == null || code == null) return;

            if (state == _state)
            {
                PerformCodeExchange(code);
            }
            else
            {
                Log("Unexpected response.");
            }
        }

        private static void PerformCodeExchange(string code)
        {
            var redirectUri = _implementation == Implementation.AuthorizationMiddleware ? AuthorizationMiddleware.Endpoint + "/redirect" : _redirectUri;
            var formFields = new Dictionary<string, string>
            {
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", Settings.Instance.ClientId },
                { "code_verifier", _codeVerifier },
                { "scope", string.Join(" ", Settings.Instance.AccessScopes) },
                { "grant_type", "authorization_code" }
            };

            if (_implementation == Implementation.LoopbackFlow || _implementation == Implementation.AuthorizationMiddleware)
            {
                formFields.Add("client_secret", Settings.Instance.ClientSecret);
            }

            var request = UnityWebRequest.Post(TokenEndpoint, formFields);

            Log($"Exchanging code for access token: {request.url}");

            request.SendWebRequest().completed += _ =>
            {
                if (request.error == null)
                {
                    Log($"TokenExchangeResponse={request.downloadHandler.text}");

                    TokenResponse = TokenResponse.Parse(request.downloadHandler.text);
                    SavedAuth.Instance = new SavedAuth(Settings.Instance.ClientId, TokenResponse);

                    if (_callbackT != null)
                    {
                        _callbackT(true, null, TokenResponse);
                    }

                    if (_callbackU != null)
                    {
                        RequestUserInfo(TokenResponse.AccessToken, _callbackU);
                    }
                }
                else
                {
                    _callbackU(false, $"{request.error}: {request.downloadHandler.text}", null);
                }
            };
        }

        /// <summary>
        /// You can move this function to your backend for more security.
        /// </summary>
        public static void RequestUserInfo(string accessToken, Action<bool, string, UserInfo> callback)
        {
            var request = UnityWebRequest.Get(UserInfoEndpoint);

            Log($"Requesting user info: {request.url}");

            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.SendWebRequest().completed += _ =>
            {
                if (request.error == null)
                {
                    Log($"UserInfo={request.downloadHandler.text}");
                    SavedAuth.Instance.UserInfo = JsonUtility.FromJson<UserInfo>(request.downloadHandler.text);
                    SavedAuth.Instance.Save();
                    callback(true, null, SavedAuth.Instance.UserInfo);
                }
                else
                {
                    callback(false, $"{request.error}: {request.downloadHandler.text}", null);
                }
            };
        }

        /// <summary>
        /// https://developers.google.com/identity/protocols/oauth2/native-app#offline
        /// </summary>
        public static void RefreshAccessToken(Action<bool, string, TokenResponse> callback)
        {
            if (SavedAuth == null) throw new Exception("Initial authorization is required.");

            var refreshToken = SavedAuth.TokenResponse.RefreshToken;
            var formFields = new Dictionary<string, string>
            {
                { "client_id", Settings.Instance.ClientId },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" }
            };

            if (_implementation == Implementation.LoopbackFlow || _implementation == Implementation.AuthorizationMiddleware)
            {
                formFields.Add("client_secret", Settings.Instance.ClientSecret);
            }

            var request = UnityWebRequest.Post(TokenEndpoint, formFields);

            Log($"Access token refresh: {request.url}");

            request.SendWebRequest().completed += obj =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Log($"TokenExchangeResponse={request.downloadHandler.text}");

                    TokenResponse = TokenResponse.Parse(request.downloadHandler.text);
                    TokenResponse.RefreshToken = refreshToken;
                    SavedAuth.Instance.TokenResponse = TokenResponse;
                    SavedAuth.Instance.Save();
                    callback(true, null, TokenResponse);
                }
                else
                {
                    callback(false, $"{request.error}: {request.downloadHandler.text}", null);
                }

                request.Dispose();
            };
        }

        private static void RevokeAccessToken(string accessToken)
        {
            var request = UnityWebRequest.PostWwwForm($"{RevocationEndpoint}?token={accessToken}", "");

            Log($"Revoking access token: {request.url}");

            request.SendWebRequest().completed += _ => Log(request.error ?? "Access token revoked!");
        }

        private static void Log(string message)
        {
            Debug.Log(message); // TODO: Remove in Release.
        }
    }
}