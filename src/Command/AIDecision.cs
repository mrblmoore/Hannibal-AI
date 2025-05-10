using System;

namespace HannibalAI.Command
{
    /// <summary>
    /// Represents a decision made by the AI system that contains a command to execute
    /// </summary>
    public class AIDecision
    {
        /// <summary>
        /// The command to execute as part of this decision
        /// </summary>
        public AICommand Command { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public AIDecision()
        {
        }

        /// <summary>
        /// Creates a new AI decision with the specified command
        /// </summary>
        /// <param name="command">The command to execute</param>
        public AIDecision(AICommand command)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }
    }
}