import re

filename = 'src/Tactics/TacticalPlanner.cs'

with open(filename, 'r') as file:
    content = file.read()

# Replace formation.Direction with Vec3 conversion
content = re.sub(
    r'teamDir \+= formation\.Direction;',
    r'teamDir += new Vec3(formation.Direction.X, formation.Direction.Y, 0);',
    content
)

# Replace ArrangementOrder comparison with string comparison
content = re.sub(
    r'enemyFormation\.ArrangementOrder\.OrderType != ArrangementOrder\.ArrangementOrderEnum\.Square',
    r'!enemyFormation.ArrangementOrder.OrderType.ToString().Contains("Square")',
    content
)

with open(filename, 'w') as file:
    file.write(content)

print("Fixed Vec2 to Vec3 conversions in TacticalPlanner.cs")
