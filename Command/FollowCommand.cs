using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class FollowCommand : AICommand
    {
        public Formation Formation { get; private set; }
        public Agent Target { get; private set; }

        public FollowCommand(Formation formation, Agent target)
            : base("Follow", CommandType.Follow)
        {
            Formation = formation;
            Target = target;
        }
    }
}
