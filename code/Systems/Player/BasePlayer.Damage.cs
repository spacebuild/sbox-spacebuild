namespace Sandbox.Systems.Player;

public partial class BasePlayer
{
	
	/// <summary>
	/// The information for the last piece of damage this player took.
	/// </summary>
	public DamageInfo LastDamage { get; protected set; }
	
	
	public override void TakeDamage( DamageInfo info )
	{
		if ( LifeState != LifeState.Alive )
			return;

		// Check for headshot damage
		var isHeadshot = info.Hitbox.HasTag( "head" );
		if ( isHeadshot )
		{
			info.Damage *= 2.5f;
		}

		// Check if we got hit by a bullet, if we did, play a sound.
		if ( info.HasTag( "bullet" ) )
		{
			Sound.FromScreen( To.Single( Client ), "sounds/player/damage_taken_shot.sound" );
		}

		// Play a deafening effect if we get hit by blast damage.
		if ( info.HasTag( "blast" ) )
		{
			SetAudioEffect( To.Single( Client ), "flasthbang", info.Damage.LerpInverse( 0, 60 ) );
		}

		if ( Health > 0 && info.Damage > 0 )
		{
			Health -= info.Damage;

			if ( Health <= 0 )
			{
				Health = 0;
				OnKilled();
			}
		}

		this.ProceduralHitReaction( info );
	}
	
	public override void OnKilled()
	{
		if ( LifeState == LifeState.Alive )
		{
			if ( LastDamage.HasTag( "vehicle" ) )
			{
				Particles.Create( "particles/impact.flesh.bloodpuff-big.vpcf", LastDamage.Position );
				Particles.Create( "particles/impact.flesh-big.vpcf", LastDamage.Position );
				PlaySound( "kersplat" );
			}
			
			CreateRagdoll( Controller.Velocity, LastDamage.Position, LastDamage.Force,
				LastDamage.BoneIndex, LastDamage.HasTag( "bullet" ), LastDamage.HasTag( "blast" ) );

			LifeState = LifeState.Dead;
			EnableAllCollisions = false;
			EnableDrawing = false;

			Controller.Remove();
			Animator.Remove();
			Inventory.Remove();
			Camera.Remove();

			// Disable all children as well.
			Children.OfType<ModelEntity>()
				.ToList()
				.ForEach( x => x.EnableDrawing = false );

			AsyncRespawn();
		}
	}
	
	
	
	[ConCmd.Server( "kill" )]
	public static void DoSuicide()
	{
		if ( ConsoleSystem.Caller.Pawn is BasePlayer player ) {
			player.TakeDamage( DamageInfo.Generic( 1000f ) );
		}
	}

	[ConCmd.Admin( "sethp" )]
	public static void SetHP( float value )
	{
		if ( ConsoleSystem.Caller.Pawn is BasePlayer player ) {
			player.Health = value;
		}
	}
	
	
}
