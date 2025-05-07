using TaleWorlds.MountAndBlade;

namespace HannibalAI.AI
{
    public interface AICommander
    {
        void AttackFormation(Formation formation, Formation targetFormation);
        void ChangeFormation(Formation formation, Formation newFormation);
        void MoveFormation(Formation formation, Vec2 position);
    }
} 