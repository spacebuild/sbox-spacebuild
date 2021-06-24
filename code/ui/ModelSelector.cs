using System.Linq;
using System.Collections.Generic;
using Sandbox.UI.Tests;

namespace Sandbox.UI
{
	[Library]
	public partial class ModelSelector : Panel
	{
		// Todo: (optionally) loading these from flat files (or json?) would be nice for model-only packs
		private static readonly Dictionary<string, List<string>> SpawnLists = new();
		VirtualScrollPanel Canvas;

		public ModelSelector( IEnumerable<string> spawnListNames )
		{
			AddClass( "modelselector" );
			AddClass( "active" );
			AddChild( out Canvas, "canvas" );

			Canvas.Layout.AutoColumns = true;
			Canvas.Layout.ItemWidth = 64;
			Canvas.Layout.ItemHeight = 64;
			Canvas.OnCreateCell = ( cell, data ) => {
				var file = (string)data;
				var panel = cell.Add.Panel( "icon" );
				panel.AddEvent( "onclick", () => {
					var currentTool = ConsoleSystem.GetValue( "tool_current" );
					ConsoleSystem.Run( $"{currentTool}_model", file );
				} );
				panel.Style.BackgroundImage = Texture.Load( $"/{file}_c.png", false );
			};

			var spawnList = spawnListNames.SelectMany( name => SpawnLists.GetValueOrDefault( name, new List<string>() ) );

			foreach ( var file in spawnList ) {
				if ( !FileSystem.Mounted.FileExists( file + "_c.png" ) ) {
					continue;
				}
				Canvas.AddItem( file );
			}
		}

		public static void AddToSpawnlist( string list, string model )
		{
			SpawnLists.GetOrCreate( list ).Add( model );
		}
		public static void AddToSpawnlist( string list, IEnumerable<string> models )
		{
			SpawnLists.GetOrCreate( list ).AddRange( models );
		}
	}
}
