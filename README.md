# Dungeons & Dragons Console Prototype

A C# console prototype for a D&D-inspired fantasy RPG. This is not currently a Unity project or a finished game.

## Status

The project contains basic character creation and a simple turn-based combat prototype. Gameplay and rules are still under development.

## Current Features

- Character creation: enter a name and choose a race and class.
- Player assigns a standard array of six ability scores during character creation.
- Race ability bonuses and class-specific ability actions.
- Equipment catalog with weapon and armor equipping.
- Turn-based combat with target selection, defend, class ability, and consumable actions.
- Healing and mana consumables, rest, dice rolling, experience, and levels.
- Forest exploration, random encounters, Goblin King quest.
- JSON save/load for character progression, equipment, consumables, and quest progress.
- Console presentation and automated unit tests.

## Known Limitations

- Combat currently uses a simplified initiative/order model and one enemy turn per round.
- Class abilities and race bonuses are prototype rules, not full D&D rules.
- Inventory supports consumables; weapon/armor collection and equipment switching are not implemented.
- Exploration has a short fixed route and one quest, not a branching campaign.
- Save files are local JSON and do not persist every possible game detail (for example, temporary combat states).
- The console interface is a prototype; the game has no 2D/3D graphics or Unity integration.
- Automated tests do not cover the full interactive game flow.

## Technology

- C#
- .NET 8
- Console application
- xUnit tests

## Run

Install the .NET 8 SDK, then run from the repository root:

```sh
dotnet run
```

The test project can be checked with:

```sh
dotnet test DnD.Tests/DnD.Tests.csproj
```

## Remaining Priorities

1. Expand combat rules, initiative, spell list, and balance.
2. Add equipment inventory and switching, shops, and more consumables.
3. Expand exploration, branching quests, story, and encounters.
4. Add automated coverage for interactive game flows and save-file edge cases.
5. Decide whether to keep the console game or move to a graphical engine.

## Contributors

- [Dwinanda](https://github.com/Dwinan)
- [Rifki Yudika](https://github.com/Ryper29)
- [Riffa](https://github.com/RiffaYP)

## Note

The previous OOP assignment code is stored on the `archive` branch. The `main` branch is intended for development of this DND game project.
