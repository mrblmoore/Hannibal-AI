using System;
using System.Collections.Generic;
using HannibalAI.Command;

namespace HannibalAI.Battle
{
    public class AIDecision
    {
        public string Action { get; set; }
        public AICommand[] Commands { get; set; }
        public string Reasoning { get; set; }

        public AIDecision()
        {
            Action = "none";
            Commands = Array.Empty<AICommand>();
            Reasoning = string.Empty;
        }

        public AIDecision(string action, AICommand[] commands, string reasoning)
        {
            Action = action;
            Commands = commands;
            Reasoning = reasoning;
        }
    }
} 