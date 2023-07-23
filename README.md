# SandboxPlus, a base gamemode for S&box

It closely follows [Facepunch's Sandbox gamemode](https://github.com/Facepunch/sandbox), with additions to make it more extendable by other modules (adding events, publicizing UI globals, etc), while staying unopinionated/light. This module is primarily aiming to become a "Gmod 2", replicating the functionality found in Gmod's Sandbox gamemode (with improvements).

## Improvements over FP's Sandbox:

- Toolgun model (wip)
- `IStopUsing` entity interface
- `SandboxPlayer.OnSimulate` event, so ents can subscribe to keypresses
- Sandbox.Hooks.Entities.OnSpawned event, Sandbox.Hooks.Undos.OnAddUndo event
- Constraint tool: an omni-tool with all your classic Gmod constraints (weld/axis/rope/elastic/slider/ballsocket)
- Material tool
- DynPlates spawnmenu: like PHX, but dynamic, fully customizable sizes using procedural meshes
- UI tool panels, including ModelSelector with extendable `.spawnlist`'s

### Todo

- [] Figure out how 2023 addons work, get Wirebox loading
- [] load toolmenus on tool switch (currently only loads when switching to the Toolgun from another; broken as of 2023 api's)
- [] Improve spawnmenu/tool hotreloading (broken as of 2023 api's, as [Event.Hotload] doesn't seem to trigger in Game.cs if it was a toolmenu.cs that updated)
- [] Restore DynPlates spawnmenu (broken as of 2023 api's)
- [] Restore Material toolmenu (broken as of 2023 api's)

## Contributing

PR's are appreciated!
