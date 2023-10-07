using Sandbox.Physics;

namespace Sandbox.Systems.Player.Animations;

public partial class BasePlayerAnimator: EntityComponent<BasePlayer>, ISingletonComponent
{
	
	public BasePlayer Player => Entity;
	public Controller.BasePlayerController Controller => Player.Controller;
	
	public virtual void Simulate( IClient cl )
	{
		CitizenAnimationHelper animHelper = new CitizenAnimationHelper( Player );

		animHelper.WithWishVelocity( Controller.WishVelocity );
		animHelper.WithVelocity( Controller.Velocity );
		animHelper.WithLookAt( Player.EyePosition + Player.EyeRotation.Forward * 100.0f, 1.0f, 1.0f, 0.5f );
		animHelper.AimAngle = Player.EyeRotation;
		animHelper.FootShuffle = 0f;
		animHelper.DuckLevel = MathX.Lerp( animHelper.DuckLevel, 1 - Controller.CurrentEyeHeight.Remap( 30, 72, 0, 1 ).Clamp( 0, 1 ), Time.Delta * 10.0f );
		animHelper.VoiceLevel = (Game.IsClient && cl.IsValid()) ? cl.Voice.LastHeard < 0.5f ? cl.Voice.CurrentLevel : 0.0f : 0.0f;
		animHelper.IsGrounded = Controller.GroundEntity != null;
		animHelper.IsSwimming = Player.GetWaterLevel() >= 0.5f;
		animHelper.IsWeaponLowered = false;

		var cariable = Player.ActiveCariable;
		if ( cariable.IsValid() ) {
			Player.SetAnimParameter( "holdtype", (int)cariable.HoldType );
			Player.SetAnimParameter( "holdtype_handedness", (int)cariable.Handedness );
			animHelper.AimBodyWeight = cariable.AimBodyWeight;
		}
	}
	
}
