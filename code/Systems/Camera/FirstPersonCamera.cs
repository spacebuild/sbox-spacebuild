using Sandbox.Systems.Player;

namespace Sandbox.Systems.Camera; 

public partial class FirstPersonCamera: EntityComponent<BasePlayer>, ISingletonComponent, IPlayerCamera {
	
	public BasePlayer Player => Entity;
	
	public virtual void Update( ) {
		Sandbox.Camera.Rotation = Player.EyeRotation;
		Sandbox.Camera.FieldOfView = Game.Preferences.FieldOfView;
		Sandbox.Camera.Position = Player.EyePosition;
		Sandbox.Camera.FieldOfView = Game.Preferences.FieldOfView;
		Sandbox.Camera.FirstPersonViewer = Player;
		Sandbox.Camera.ZNear = 0.5f;
	}
	
}
