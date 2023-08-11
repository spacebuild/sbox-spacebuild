using System;

namespace Sandbox.Tools
{
	[Library( "tool_color", Title = "Color", Description = "Change render color and alpha of entities", Group = "construction" )]
	public partial class ColorTool : BaseTool
	{
		public override void Simulate()
		{
			if ( !Game.IsServer )
				return;

			using ( Prediction.Off() )
			{
				var startPos = Owner.EyePosition;
				var dir = Owner.EyeRotation.Forward;

				var color = Color.Random;

				if ( Input.Pressed( "attack1" ) )
				{
					var tr = DoTrace();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					if ( tr.Entity is not ModelEntity modelEnt )
						return;

					modelEnt.RenderColor = Color.Random;

					CreateHitEffects( tr.EndPosition );
				}
				//prob awful way of doing it.
				if ( Input.Pressed( "attack2" ) )
				{
					var tr = DoTrace();

					if ( !tr.Hit || !tr.Entity.IsValid() )
						return;

					if ( tr.Entity is not ModelEntity modelEnt )
						return;

					modelEnt.RenderColor = Color.White;

					CreateHitEffects( tr.EndPosition );
				}
			}
		}
	}
}
