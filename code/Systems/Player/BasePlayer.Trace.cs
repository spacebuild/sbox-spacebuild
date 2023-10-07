using System.ComponentModel;

namespace Sandbox.Systems.Player;

public partial class BasePlayer
{

	public TraceResult TraceRay( float maxDistance ) {
		var startPos = EyePosition;
		var dir = EyeRotation.Forward;

		var tr = Trace.Ray( startPos, startPos + (dir * maxDistance) )
			.WithAnyTags( "solid", "nocollide" )
			.Ignore( Owner )
			.Run();
	}
	
	
}
