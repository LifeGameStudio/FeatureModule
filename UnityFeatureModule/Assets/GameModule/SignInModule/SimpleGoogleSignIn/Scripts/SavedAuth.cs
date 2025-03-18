using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace Assets.SimpleGoogleSignIn.Scripts
{
    public class SavedAuth
    {
        public string ClientId;
        public TokenResponse TokenResponse;
        public UserInfo UserInfo;

        private static SavedAuth _instance;

        public static SavedAuth Instance
        {
            get => _instance ??= GetInstance();
            set { _instance = value; _instance.Save(); }
        }

        [Preserve]
        private SavedAuth()
        {
        }

        public SavedAuth(string clientId, TokenResponse tokenResponse)
        {
            ClientId = clientId;
            TokenResponse = tokenResponse;
        }

        public void Save()
        {
            PlayerPrefs.SetString(typeof(SavedAuth).FullName, JsonConvert.SerializeObject(this));
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            _instance = null;
            PlayerPrefs.DeleteKey(typeof(SavedAuth).FullName);
            PlayerPrefs.Save();
        }

        private static SavedAuth GetInstance()
        {
            if (!PlayerPrefs.HasKey(typeof(SavedAuth).FullName)) return null;

            try
            {
                return JsonConvert.DeserializeObject<SavedAuth>(PlayerPrefs.GetString(typeof(SavedAuth).FullName));
            }
            catch
            {
                return null;
            }
        }
    }
}