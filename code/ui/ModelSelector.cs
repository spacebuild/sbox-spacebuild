using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Sandbox.UI.Tests;
using System.Text.RegularExpressions;

namespace Sandbox.UI
{
	[Library]
	public partial class ModelSelector : Panel
	{
		// Todo: (optionally) loading these from flat files (or json?) would be nice for model-only packs
		private static readonly Dictionary<string, List<string>> SpawnLists = new();
		VirtualScrollPanel Canvas;

		private static readonly Regex reModelMatGroup = new( @"^(.*?)(?:--(\d+))?\.vmdl$" );
		public ModelSelector( IEnumerable<string> spawnListNames, bool showMaterialGroups = false )
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
				var match = reModelMatGroup.Match( file );
				panel.AddEventListener( "onclick", () => {
					var currentTool = ConsoleSystem.GetValue( "tool_current" );
					ConsoleSystem.Run( $"{currentTool}_model", match.Groups[1] + ".vmdl" );
					ConsoleSystem.Run( $"{currentTool}_materialgroup", match.Groups.Count > 2 ? match.Groups[2] : 0 );
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

		public static IEnumerable<string> GetSpawnList( string list )
		{
			return SpawnLists.GetOrCreate( list );
		}
	}
}
