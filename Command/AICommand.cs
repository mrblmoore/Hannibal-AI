using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class AICommand
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string SubType { get; set; }
        public int FormationIndex { get; set; }
        public int? TargetFormationIndex { get; set; }
        public Vec3 TargetPosition { get; set; }
        public object[] Parameters { get; set; }

        public AICommand()
        {
            Parameters = Array.Empty<object>();
        }

        public AICommand(string type, int formationIndex)
        {
            Type = type;
            FormationIndex = formationIndex;
            Parameters = Array.Empty<object>();
        }

        public AICommand(string type, int formationIndex, Vec3 targetPosition)
        {
            Type = type;
            FormationIndex = formationIndex;
            TargetPosition = targetPosition;
            Parameters = Array.Empty<object>();
        }

        public AICommand(string type, int formationIndex, object[] parameters)
        {
            Type = type;
            FormationIndex = formationIndex;
            Parameters = parameters ?? Array.Empty<object>();
        }

        public void Execute(Mission mission)
        {
            var executor = new CommandExecutor(mission);
            executor.ExecuteCommand(this);
        }
    }

    public class AIDecision
    {
        public string Action { get; set; }
        public AICommand[] Commands { get; set; }
        public string Reasoning { get; set; }
        public List<string> TacticsUsed { get; set; }

        public AIDecision()
        {
            Commands = Array.Empty<AICommand>();
        }

        public AIDecision(string action, string reasoning, AICommand[] commands)
        {
            Action = action;
            Reasoning = reasoning;
            Commands = commands ?? Array.Empty<AICommand>();
        }
    }
} 