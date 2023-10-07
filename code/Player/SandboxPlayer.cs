using Sandbox.Systems.Inventory;
using Sandbox.Systems.Player;

namespace Sandbox.Player; 

public partial class SandboxPlayer: BasePlayer {
	public override void Respawn() {
		base.Respawn();
		
		Inventory.Add( new PhysGun(), true );
		Inventory.Add( new GravGun() );
		Inventory.Add( new Tool() );
		Inventory.Add( new Pistol() );
		Inventory.Add( new MP5() );
		Inventory.Add( new Flashlight() );
		Inventory.Add( new Fists() );
	}

	public override void TakeDamage( DamageInfo info ) {
		
		if ( LifeState != LifeState.Alive )
			return;
		
		if ( info.Attacker.IsValid() )
		{
			if ( info.Attacker.Tags.Has( $"{PhysGun.GrabbedTag}{Client.SteamId}" ) )
				return;
		}
		
		
		base.TakeDamage(info);

	}
	
	[Event( "entity.spawned" )]
	public static void OnSpawned( Entity spawned, Entity owner )
	{
		if ( owner is BasePlayer player )
		{
			spawned.SetPlayerOwner( player );
		}
	}
}
