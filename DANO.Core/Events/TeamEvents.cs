namespace DANO.Events
{
    /// <summary>プレイヤーのチームが変更されたときのイベント</summary>
    public class TeamChangedEvent
    {
        public int PlayerId { get; }
        public int OldTeamId { get; }
        public int NewTeamId { get; }

        internal TeamChangedEvent(int playerId, int oldTeamId, int newTeamId)
        {
            PlayerId = playerId;
            OldTeamId = oldTeamId;
            NewTeamId = newTeamId;
        }
    }
}
