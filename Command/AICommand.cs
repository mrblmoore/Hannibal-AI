using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public abstract class AICommand
    {
        public CommandType Type { get; private set; }

        protected AICommand(CommandType type)
        {
            Type = type;
        }
    }

    public enum CommandType
    {
        Movement,
        Engagement,
        FormationChange,
        Miscellaneous
    }
}
