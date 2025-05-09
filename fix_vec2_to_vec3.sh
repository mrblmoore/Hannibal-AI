#!/bin/bash

# Fix Vec2 to Vec3 conversion in TacticalPlanner.cs
sed -i 's/teamDir += formation.Direction;/teamDir += new Vec3(formation.Direction.X, formation.Direction.Y, 0);/g' src/Tactics/TacticalPlanner.cs
sed -i 's/enemyDir += enemyFormation.Direction;/enemyDir += new Vec3(enemyFormation.Direction.X, enemyFormation.Direction.Y, 0);/g' src/Tactics/TacticalPlanner.cs

# Fix OrderType comparison in TacticalPlanner
sed -i 's/enemyFormation.ArrangementOrder.OrderType != ArrangementOrder.ArrangementOrderEnum.Square/enemyFormation.ArrangementOrder.OrderType.ToString().Contains("Square") == false/g' src/Tactics/TacticalPlanner.cs

# Fix the TerrainAnalyzer Reset method that doesn't exist
echo '
        /// <summary>
        /// Reset terrain analysis for a new battle
        /// </summary>
        public void Reset()
        {
            // Clear cached terrain data
            Logger.Instance.Info("TerrainAnalyzer reset for new battle");
        }' >> src/Terrain/TerrainAnalyzer.cs

chmod +x fix_vec2_to_vec3.sh
./fix_vec2_to_vec3.sh

echo "Fixes applied."
