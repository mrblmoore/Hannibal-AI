#!/bin/bash

# Script to fix Vec3 conversion in AIService.cs
echo "Fixing Vec3 conversion in AIService.cs..."

# Replace formation positions with GetFormationPosition
sed -i 's/formation\.CurrentPosition\.ToVec3()/GetFormationPosition(formation)/g' src/AIService.cs
sed -i 's/enemyFormation\.CurrentPosition\.ToVec3()/GetFormationPosition(enemyFormation)/g' src/AIService.cs
sed -i 's/otherFormation\.CurrentPosition\.ToVec3()/GetFormationPosition(otherFormation)/g' src/AIService.cs
sed -i 's/archerFormation\.CurrentPosition\.ToVec3()/GetFormationPosition(archerFormation)/g' src/AIService.cs
sed -i 's/smallestInfantry\.CurrentPosition\.ToVec3()/GetFormationPosition(smallestInfantry)/g' src/AIService.cs

# Replace agent positions with GetAgentPosition
sed -i 's/Mission\.Current\.MainAgent\.Position\.ToVec3()/GetAgentPosition(Mission.Current.MainAgent)/g' src/AIService.cs

# Replace remaining Vec3 calls
sed -i 's/GetVec3(formation\.CurrentPosition)/GetFormationPosition(formation)/g' src/AIService.cs
sed -i 's/GetVec3(enemyFormation\.CurrentPosition)/GetFormationPosition(enemyFormation)/g' src/AIService.cs
# Note: The regex with capture groups is causing issues, we'll handle these cases manually

echo "Fix complete. Please build the project now."