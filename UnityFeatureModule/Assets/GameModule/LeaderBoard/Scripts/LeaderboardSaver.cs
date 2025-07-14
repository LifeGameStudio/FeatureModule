namespace GameModule.LeaderBoard.Scripts
{
    using UnityEngine;

    public class LeaderboardSaver
    {
        public static string UserName
        {
            get => PlayerPrefs.GetString("LeaderboardUserName", string.Empty);
            set => PlayerPrefs.SetString("LeaderboardUserName", value);
        }
        
        public static int UserScore
        {
            get => PlayerPrefs.GetInt("LeaderboardUserScore", 0);
            set => PlayerPrefs.SetInt("LeaderboardUserScore", value);
        }
    }
}
