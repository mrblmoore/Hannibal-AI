using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class FollowCommand : AICommand
    {
        public Formation Follower { get; private set; }
        public Formation Leader { get; private set; }

        public FollowCommand(Formation follower, Formation leader)
            : base(CommandType.Movement)
        {
            Follower = follower;
            Leader = leader;
        }
    }
}
