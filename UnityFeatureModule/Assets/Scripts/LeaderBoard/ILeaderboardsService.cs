namespace Game.Scripts.LeaderBoard
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;

    public interface ILeaderboardsService
    {
        List<LeaderBoardEntry> Entries();
        
        UniTask SubmitScore(LeaderBoardEntry config);
    }

    [System.Serializable]
    public class LeaderBoardEntry
    {
        public string userName;
        public int    score;
    }
}