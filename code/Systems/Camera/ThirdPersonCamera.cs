using Sandbox.Systems.Player;

namespace Sandbox.Systems.Camera; 

public partial class ThirdPersonCamera: EntityComponent<BasePlayer>, ISingletonComponent, IPlayerCamera {
	
	public BasePlayer Player => Entity;
	
	public virtual void Update() {
		Sandbox.Camera.Rotation = Player.EyeRotation;
		Sandbox.Camera.FieldOfView = Game.Preferences.FieldOfView;
		var up = -Player.GravityDirection;
		Sandbox.Camera.FirstPersonViewer = null;
			
		var center = Player.Position + (up  * 64);

		var pos = center;
		var rot = Rotation.FromAxis( up, -16 ) * Sandbox.Camera.Rotation;

		float distance = 130.0f * Player.Scale;
		var targetPos = pos + rot.Right * ((Player.CollisionBounds.Mins.x + 32) * Player.Scale);
		targetPos += rot.Forward * -distance;

		var tr = Trace.Ray( pos, targetPos )
			.WithAnyTags( "solid" )
			.Ignore( Player )
			.Radius( 8 )
			.Run();

		Sandbox.Camera.Position = tr.EndPosition;
	}
	
}
