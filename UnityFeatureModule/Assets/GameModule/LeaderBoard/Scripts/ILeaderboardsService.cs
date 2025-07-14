namespace GameModule.LeaderBoard.Scripts
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;

    public interface ILeaderboardsService
    {
        string                 LeaderboardId { get; set; }
        string                 UserId        { get; set; }
        string                 UserName      { get; set; }
        void                   Init(string idToken, string accessToken, string leaderboardId = "leaderboard");
        void                   FetchLimitEntries(int limit = 10);
        void                   FetchAllEntries();
        List<LeaderBoardEntry> LimitEntries();
        List<LeaderBoardEntry> AllEntries();

        UniTask SubmitScore(int score);
        UniTask UpdateUserName(string userName);
    }

    [System.Serializable]
    public class LeaderBoardEntry
    {
        public string userName;
        public int    score;
    }
}
