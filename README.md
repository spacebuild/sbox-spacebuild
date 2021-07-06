# SandboxPlus, a base gamemode module for S&box

S&Box is currently very "gamemode" focused, with no official support for traditional "addons".  
SandboxPlus works with [[A]lex's minimal-extended Modular Framework](https://github.com/Ceveos/minimal-extended) as a "base module" which other modules (eg. [Wirebox](https://github.com/wiremod/wirebox)) can extend.

It closely follows [Facepunch's Sandbox gamemode](https://github.com/Facepunch/sandbox), with additions to make it more extendable by other modules (adding events, publicizing UI globals, etc), while staying unopinionated/light. This module is primarily aiming to become a "Gmod 2", replicating the functionality found in Gmod's Sandbox gamemode (with improvements).

## Improvements over FP's Sandbox:

- restructured to fit [minimal-extended Modular Framework](https://github.com/Ceveos/minimal-extended)
- Toolgun model (wip)
- `IStopUsing` entity interface
- `SandboxPlayer.OnSimulate` event, so ents can subscribe to keypresses
- Sandbox.Hooks.Entities.OnSpawned event, Sandbox.Hooks.Undos.OnAddUndo event
- Constraint tool: an omni-tool with all your classic Gmod constraints (weld/axis/rope/elastic/slider/ballsocket)
- Material tool
- DynPlates spawnmenu: like PHX, but dynamic, fully customizable sizes using procedural meshes
- All Sandbox UI hotreloads, including spawnmenu's, tools, and other SandboxHUD elements
- UI tool panels, including ModelSelector with extendable `.spawnlist`'s

## Contributing

PR's are appreciated!
### Building from scratch

1. `cd steamapps/common/sbox/`
2. `git clone https://github.com/Ceveos/minimal-extended.git workspace && cd workspace`
3. `.\watcher.ps1 -create`, say yes to prompt to download SandboxPlus
4. (optional) `git clone https://github.com/wiremod/wirebox.git modules/wirebox` (and any other modules you want)
5. `.\watcher.ps1 your-gamemode -build`

The watcher script will combine all workspace/modules/* (so you can throw extra mods there), and output a new "gamemode" into `steamapps/sbox/addons/your-gamemode/`, where S&Box is looking for it.
