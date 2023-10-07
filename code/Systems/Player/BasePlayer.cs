using Sandbox.Systems.Camera;
using Sandbox.Systems.Inventory;
using Sandbox.Systems.Player.Animations;

namespace Sandbox.Systems.Player;

public partial class BasePlayer: AnimatedEntity
{
	/**
	 * System controllers
	 */
	
	/// <summary>
	/// The controller is responsible for player movement and setting up EyePosition / EyeRotation.
	/// </summary>
	[BindComponent] public Controller.BasePlayerController Controller { get; }

	/// <summary>
	/// The animator is responsible for animating the player's current model.
	/// </summary>
	[BindComponent] public BasePlayerAnimator Animator { get; }
	
	/// <summary>
	/// The player's camera.
	/// </summary>
	[BindComponent] public IPlayerCamera Camera { get; }
	
	/**
	 * Inventory
	 */
	
	/// <summary>
	/// The inventory is responsible for storing cariable items for a player to use.
	/// </summary>
	[BindComponent] public BasePlayerInventory Inventory { get; }
	
	/// <summary>
	/// Accessor for getting a player's active cariable item.
	/// </summary>
	public ICariable ActiveCariable => Inventory?.ActiveCariable;
	
	/*
	 * Gravity
	 */
	
	/// <summary>
	/// Active gravitational force
	/// </summary>
	[Net] public Vector3 Gravity { get; set; } = Game.PhysicsWorld.Gravity;
	/// <summary>
	/// Active gravitational force direction
	/// </summary>
	[Net] public Vector3 GravityDirection { get; set; } = Game.PhysicsWorld.Gravity.Normal;
	
	/**
	 * Inputs
	 */
	[ClientInput] public ICariable ActiveCariableInput { get; set; }
	
	
	/// <summary>
	/// Called every server and client tick.
	/// </summary>
	/// <param name="cl"></param>
	public override void Simulate( IClient cl )
	{
		Controller?.Simulate( cl );
		Animator?.Simulate( cl );
		Inventory?.Simulate( cl );
	}
	
	/// <summary>
	/// Called every frame clientside.
	/// </summary>
	/// <param name="cl"></param>
	public override void FrameSimulate( IClient cl )
	{
		if ( Input.Pressed( InputButton.View ) ) {
			var isFirstPerson = Camera is FirstPersonCamera;
			Camera.Remove();
			if ( isFirstPerson) {
				Components.Create<ThirdPersonCamera>();
			}else {
				Components.Create<FirstPersonCamera>();
			}
		}
		
		Controller?.FrameSimulate( cl );
		Camera?.Update( );
	}
	
	
	
	
	
	
	
}
