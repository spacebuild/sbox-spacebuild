using Sandbox.Systems.Camera;
using Sandbox.Systems.Inventory;
using Sandbox.Systems.Player.Animations;

namespace Sandbox.Systems.Player;

public partial class BasePlayer
{
	
	/// <summary>
	/// The model your player will use.
	/// </summary>
	static Model PlayerModel = Model.Load( "models/citizen/citizen.vmdl" );

	/// <summary>
	/// When the player is first created. This isn't called when a player respawns.
	/// </summary>
	public override void Spawn()
	{
		Model = PlayerModel;
		Predictable = true;

		// Default properties
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		EnableLagCompensation = true;
		EnableHitboxes = true;

		Tags.Add( "player" );
	}
	
	/// <summary>
	/// Called when a player respawns, think of this as a soft spawn - we're only reinitializing transient data here.
	/// </summary>
	public virtual void Respawn()
	{
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -16, -16, 0 ), new Vector3( 16, 16, 72 ) );

		this.ClearWaterLevel();
		Health = 100;
		LifeState = LifeState.Alive;
		EnableAllCollisions = true;
		EnableDrawing = true;

		// Re-enable all children.
		Children.OfType<ModelEntity>()
			.ToList()
			.ForEach( x => x.EnableDrawing = true );

		// We need a player controller to work with any kind of mechanics.
		Components.Create<Controller.BasePlayerController>();
		Velocity = Vector3.Zero;

		Components.Create<BasePlayerAnimator>();
		Components.Create<FirstPersonCamera>();
		Components.Create<BasePlayerInventory>();

		SetupClothing();

		GameManager.Current?.MoveToSpawnpoint( this );
		ResetInterpolation();
	}
	
	private async void AsyncRespawn()
	{
		await GameTask.DelaySeconds( 3f );
		Respawn();
	}
	
}
