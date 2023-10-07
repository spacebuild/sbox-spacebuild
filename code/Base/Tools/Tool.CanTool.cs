using Sandmod.Permission;
using Sandbox.Systems.Player;

namespace Sandbox
{
	public class CanToolParams
	{
		public BasePlayer player;
		public string toolName;
		public TraceResult tr;
		public Entity entity;
		public bool preventDefault = false;

		public static TraceResult RunCanTool( BasePlayer player, string toolName, TraceResult tr )
		{
			if ( Game.IsClient )
			{
				return tr; // PlayerOwner table is currently serverside only
			}
			if ( !tr.Entity.IsValid() || tr.Entity.IsWorld )
			{
				return tr;
			}
			if ( !player.Client.HasPermission( $"tool.{toolName}", tr.Entity ) )
			{
				return new TraceResult();
			}
			// todo: do we still want the player.cantool event, or is the HasPermission check sufficient?
			var canToolParams = new CanToolParams
			{
				player = player,
				toolName = toolName,
				tr = tr,
				entity = tr.Entity,
			};
			Event.Run( "player.cantool", canToolParams );
			if ( canToolParams.preventDefault )
			{
				return new TraceResult();
			}
			else
			{
				return tr;
			}
		}
	}
}
