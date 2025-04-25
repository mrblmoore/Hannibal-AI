using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public enum CommandType
    {
        None,
        Attack,
        Defend,
        Retreat,
        Regroup,
        ChangeFormation,
        Move
    }

    public abstract class AICommand
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string SubType { get; set; }
        public int FormationIndex { get; set; }
        public int? TargetFormationIndex { get; set; }
        public Vec3 TargetPosition { get; set; }
        public object[] Parameters { get; set; }
        public CommandType Decision { get; set; }
        public Formation Formation { get; protected set; }
        public Vec3 Position { get; protected set; }
        public float Priority { get; protected set; }

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

        protected AICommand(Formation formation, Vec3 position, float priority)
        {
            Formation = formation;
            Position = position;
            Priority = priority;
        }

        public void Execute(Mission mission)
        {
            var executor = new CommandExecutor(mission);
            executor.ExecuteCommand(this);
        }
    }

    public class ChangeFormationCommand : AICommand
    {
        public ChangeFormationCommand(Formation formation, Vec3 position, float priority)
            : base(formation, position, priority)
        {
        }
    }

    public class FlankCommand : AICommand
    {
        public FlankCommand(Formation formation, Vec3 position, float priority)
            : base(formation, position, priority)
        {
        }
    }

    public class HoldCommand : AICommand
    {
        public HoldCommand(Formation formation, Vec3 position, float priority)
            : base(formation, position, priority)
        {
        }
    }

    public class ChargeCommand : AICommand
    {
        public ChargeCommand(Formation formation, Vec3 position, float priority)
            : base(formation, position, priority)
        {
        }
    }

    public class FollowCommand : AICommand
    {
        public FollowCommand(Formation formation, Vec3 position, float priority)
            : base(formation, position, priority)
        {
        }
    }
} 