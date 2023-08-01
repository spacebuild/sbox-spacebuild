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
		private static Dictionary<string, List<string>> SpawnLists = new();
		private static bool spawnListsLoaded = false;
		VirtualScrollPanel Canvas;

		private static readonly Regex reModelMatGroup = new( @"^(.*?)(?:--(\d+))?\.vmdl$" );
		private static readonly Regex reSpawnlistFile = new( @"([^\.]+)\.spawnlist$" );
		public ModelSelector( IEnumerable<string> spawnListNames, bool showMaterialGroups = false )
		{
			AddClass( "modelselector" );
			AddClass( "active" );
			AddChild( out Canvas, "canvas" );

			Canvas.Layout.AutoColumns = true;
			Canvas.Layout.ItemWidth = 64;
			Canvas.Layout.ItemHeight = 64;
			Canvas.OnCreateCell = ( cell, data ) =>
			{
				var file = (string)data;
				var panel = cell.Add.Panel( "icon" );
				var match = reModelMatGroup.Match( file );
				panel.AddEventListener( "onclick", () =>
				{
					var currentTool = ConsoleSystem.GetValue( "tool_current" );
					ConsoleSystem.Run( $"{currentTool}_model", match.Groups[1] + ".vmdl" );
					ConsoleSystem.Run( $"{currentTool}_materialgroup", match.Groups.Count > 2 ? match.Groups[2] : 0 );
				} );
				panel.Style.BackgroundImage = Texture.Load( $"/{file}_c.png", false );
			};

			var spawnList = spawnListNames.SelectMany( GetSpawnList );

			foreach ( var file in spawnList )
			{
				if ( !FileSystem.Mounted.FileExists( file + "_c.png" ) )
				{
					continue;
				}
				Canvas.AddItem( file );
			}
		}

		/// To add models/materials to the spawnlists:
		/// either call these functions in your addon init, like `ModelSelector.AddToSpawnlist( "thruster", new string[] {"models/blah.vmdl"} )`
		/// or add an `addonname.thruster.spawnlist` file (newline delimited list of models)
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
			if ( !spawnListsLoaded )
			{
				InitializeSpawnlists();
			}
			return SpawnLists.GetOrCreate( list );
		}

		private static void InitializeSpawnlists()
		{
			spawnListsLoaded = true;
			foreach ( var file in FileSystem.Mounted.FindFile( "/", "*.spawnlist", true ) )
			{
				var match = reSpawnlistFile.Match( file );
				var listName = match.Groups[1].Value;
				var models = FileSystem.Mounted.ReadAllText( file ).Trim().Split( '\n' ).Select( x => x.Trim() );
				SpawnLists.GetOrCreate( listName ).AddRange( models );
			}
		}
	}
}
