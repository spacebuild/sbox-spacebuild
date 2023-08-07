# Custom Events provided by SandboxPlus

- "entity.spawned"
  - `Event.Run( "entity.spawned", IEntity spawned, IEntity owner )`
- "undo.add"
  - `Event.Run( "undo.add", Func<string> undo, IEntity owner )`
  - Add an "Undoable" lambda, which will be called if the player wants to undo.  
    Should return the string to show in the toast, or empty string if the undoable is redundant and should be skipped over (eg. if the weld was already removed)
- "spawnlists.initialize"
  - Takes no parameters; you're expected to call `ModelSelector.AddToSpawnlist( "screen", string[] models)`
- "player.simulate"
  - `Event.Run( "player.simulate", SandboxPlayer player )`
- "sandbox.hud.loaded"

For more usage examples, see [Wirebox](https://github.com/wiremod/wirebox).
