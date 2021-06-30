using Sandbox;
using Sandbox.UI;

namespace Sandbox.Tools
{
	[Library( "tool_material", Title = "Material", Description = "Primary: Set Material Override\nSecondary: Change Model's MaterialGroup (if supported)", Group = "construction" )]
	public partial class MaterialTool : BaseTool
	{
		[ConVar.ClientData( "tool_material_material" )]
		public static string _ { get; set; } = "";
		public override void Simulate()
		{
			using ( Prediction.Off() ) {

				var tr = DoTrace();

				if ( !tr.Hit || !tr.Entity.IsValid() )
					return;

				if ( tr.Entity is not ModelEntity modelEnt )
					return;

				if ( Input.Pressed( "attack1" ) ) {
					modelEnt.SetClientMaterialOverride( GetConvarValue( "tool_material_material" ) );

					CreateHitEffects( tr.EndPosition, tr.Normal );
				}
				else if ( Input.Pressed( "attack2" ) ) {
					modelEnt.SetMaterialGroup( modelEnt.GetMaterialGroup() + 1 );
					if ( modelEnt.GetMaterialGroup() == 0 ) {
						// cycle back to start
						modelEnt.SetMaterialGroup( 0 );
					}

					CreateHitEffects( tr.EndPosition, tr.Normal, true );
				}
			}
		}

		[ClientRpc]
		public static void SetEntityMaterialOverride( ModelEntity modelEnt, string material )
		{
			if ( Game.IsClient ) {
				modelEnt?.SceneObject?.SetMaterialOverride( Material.Load( material ) );
			}
		}

		public override void Activate()
		{
			base.Activate();

			if ( Game.IsClient ) {
				var materialSelector = new MaterialSelector();
				SpawnMenu.Instance?.ToolPanel?.AddChild( materialSelector );
			}
		}
	}

	[Library]
	public class MaterialLists : MinimalExtended.IAutoload
	{
		public MaterialLists()
		{
			UI.ModelSelector.AddToSpawnlist( "material", new string[] {
				"materials/lighthouse_concrete_floor.vmat",
				"materials/gas_station_concrete_floor.vmat",
				"materials/gas_station_concrete_wall.vmat",
				"materials/0_dirt.vmat",
				"materials/1_snow.vmat",
				"materials/2_sand.vmat",
				"materials/3_rock.vmat",
				"materials/4_grass.vmat",
				"materials/5_forest.vmat",
				"materials/6_stones.vmat",
				"materials/7_gravel.vmat",
			} );
		}
		public bool ReloadOnHotload => true;
		public void Dispose() { }
	}
}


public static partial class ModelEntityExtensions
{
	public static void SetClientMaterialOverride( this ModelEntity instance, string material )
	{
		Sandbox.Tools.MaterialTool.SetEntityMaterialOverride( instance, material );
	}
}
