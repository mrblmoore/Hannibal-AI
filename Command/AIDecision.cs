using System;

namespace HannibalAI.Command
{
    public class AIDecision
    {
        public AICommand Command { get; set; }

        public AIDecision()
        {
        }

        public AIDecision(AICommand command)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }
    }
}
