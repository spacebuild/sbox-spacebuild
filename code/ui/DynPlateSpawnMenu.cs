
using MinimalExtended;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using Sandbox.UI.Tests;

namespace Sandbox
{
	[Library]
	public class DynPlateSpawnMenu : IAutoload
	{
		public DynPlateSpawnMenu()
		{
			if ( Host.IsClient ) {
				Log.Info( "[Client] DynPlateSpawnMenu created" );

				SandboxHud.OnHudLoaded -= Initialize;
				SandboxHud.OnHudLoaded += Initialize;
			}
		}
		public void Initialize()
		{
			var plateList = SpawnMenu.Instance.SpawnMenuLeftBody.AddChild<PlateList>();
			SpawnMenu.Instance.SpawnMenuLeftTabs
				.AddButtonActive( "DynPlates", ( b ) => plateList.SetClass( "active", b ) );
		}

		public void Dispose()
		{
		}
	}

	[Library]
	public partial class PlateList : Panel
	{
		private struct DynPlateDimensions
		{
			public int x;
			public int y;
			public int z;
			public int scale;
			public DynPlateDimensions( int x, int y, int z, int scale )
			{
				this.x = x;
				this.y = y;
				this.z = z;
				this.scale = scale;
			}
		}
		VirtualScrollPanel Canvas;

		public PlateList()
		{
			AddClass( "spawnpage" );
			AddClass( "dynplates" );
			AddChild( out Canvas, "canvas" );

			Canvas.Layout.Columns = 16;
			Canvas.Layout.ItemSize = new Vector2( 64, 64 );
			Canvas.OnCreateCell = ( cell, data ) => {
				var entry = (DynPlateDimensions)data;
				var btn = cell.Add.Button( $"{entry.x}x{entry.y}" );
				btn.AddClass( "icon" );
				btn.AddEventListener( "onclick", () => ConsoleSystem.Run( "spawn_dynplate", entry.x, entry.y, entry.z, entry.scale ) );
			};

			for ( var x = 1; x <= 12; x++ ) {
				for ( var y = 12; y <= 12 * 8; y += 12 ) {
					Canvas.AddItem( new DynPlateDimensions( x * 12, y, 1, 64 ) );
				}
				for ( var y = 12 * 8 + 24; y <= 12 * 8 + 24 * 8; y += 24 ) {
					Canvas.AddItem( new DynPlateDimensions( x * 12, y, 1, 64 ) );
				}
			}
		}
	}
}
