using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class FollowCommand : AICommand
    {
        public Formation Follower { get; }
        public Formation Leader { get; }

        public FollowCommand(Formation follower, Formation leader)
        {
            Follower = follower;
            Leader = leader;
        }
    }
}
