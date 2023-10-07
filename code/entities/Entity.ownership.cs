using Sandbox.Systems.Player;

public static class EntityOwnershipExtensions
{
	public static Dictionary<Entity, BasePlayer> PlayerOwners { get; set; } = new();
	public static BasePlayer GetPlayerOwner( this Entity ent )
	{
		Game.AssertServer();
		return PlayerOwners.GetValueOrDefault( ent );
	}
	public static void SetPlayerOwner( this Entity ent, BasePlayer player )
	{
		Game.AssertServer();
		PlayerOwners[ent] = player;
	}
}
