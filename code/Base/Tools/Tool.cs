﻿using Sandbox.Systems.Player;
using Sandbox.Tools;
using Sandbox.UI;

[Library( "weapon_tool", Title = "Toolgun" )]
partial class Tool : Carriable
{
	[ConVar.ClientData( "tool_current" )]
	public static string UserToolCurrent { get; set; } = "tool_boxgun";

	public AnimatedEntity ViewModelArms { get; set; }

	[Net, Change]
	public BaseTool CurrentTool { get; set; }

	private Texture Texture;
	private ToolgunPanel Panel;
	private SceneCustomObject RenderObject;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/weapons/toolgun.vmdl" );
	}

	public override void CreateViewModel()
	{
		base.CreateViewModel();

		ViewModelEntity = new ViewModel();
		ViewModelEntity.Position = Position;
		ViewModelEntity.Owner = Owner;
		ViewModelEntity.EnableViewmodelRendering = true;
		ViewModelEntity.Model = Model.Load( "models/weapons/v_toolgun.vmdl" );

		ViewModelArms = new AnimatedEntity( "models/first_person/first_person_arms.vmdl" );
		ViewModelArms.SetParent( ViewModelEntity, true );
		ViewModelArms.EnableViewmodelRendering = true;
	}

	public override void Simulate( IClient owner )
	{
		if ( Game.IsServer )
		{
			UpdateCurrentTool( owner );
		}

		CurrentTool?.Simulate();

		if ( Game.IsServer )
		{
			CurrentTool?.UpdatePreviews();
		}
	}

	private void UpdateCurrentTool( IClient owner )
	{
		var toolName = owner.GetClientData<string>( "tool_current", "tool_balloon" );
		if ( toolName == null )
			return;

		// Already the right tool
		if ( CurrentTool != null && CurrentTool.ClassName == toolName )
			return;

		if ( CurrentTool != null )
		{
			CurrentTool?.Deactivate();
			CurrentTool = null;
		}

		CurrentTool = TypeLibrary.Create<BaseTool>( toolName );

		if ( CurrentTool != null )
		{
			CurrentTool.Parent = this;
			CurrentTool.Owner = owner.Pawn as BasePlayer;
			CurrentTool.Activate();
		}
	}

	// Note: called clientside only
	private void OnCurrentToolChanged( BaseTool oldTool, BaseTool newTool )
	{
		oldTool?.Deactivate();
		newTool?.Activate();
	}

	private void UpdateToolgunPanel()
	{
		var toolName = DisplayInfo.For( CurrentTool ).Name;
		Panel.CurrentToolName = toolName;

		//var type = DisplayInfo.For( CurrentTool ).

		if (CurrentTool is ConstraintTool ctool)
		{
			Panel.CurrentToolName = $"{toolName} | {ctool.Type.ToString()}";
		}
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );

		CurrentTool?.Activate();

		if (Game.IsClient)
		{
			RenderObject = new SceneCustomObject( Game.SceneWorld )
			{
				RenderOverride = ToolgunScreenRender
			};

			Panel = new()
			{
				RenderedManually = true,
				PanelBounds = new Rect( 0, 0, 1024, 1024 ),
			};

			UpdateToolgunPanel();

			Texture = Texture.CreateRenderTarget().WithSize( Panel.PanelBounds.Size ).Create();
		}
	}

	private void ToolgunScreenRender( SceneObject sceneObject )
	{
		Graphics.RenderTarget = RenderTarget.From( Texture );
		Graphics.Attributes.SetCombo( "D_WORLDPANEL", 0 );
		Graphics.Viewport = new Rect( 0, Panel.PanelBounds.Size );
		Graphics.Clear();

		Panel.RenderManual();

		Graphics.RenderTarget = null;
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		base.ActiveEnd( ent, dropped );

		CurrentTool?.Deactivate();

		if (Game.IsClient)
		{
			RenderObject?.Delete();
			RenderObject = null;

			Panel?.Delete();
			Panel = null;
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		CurrentTool?.Deactivate();
		CurrentTool = null;
	}

	public override void BuildInput()
	{
		CurrentTool?.BuildInput();
	}

	public override void OnDrop( Entity dropper )
	{
	}

	[Event.Client.Frame]
	public void OnFrame()
	{
		if ( Owner is Player player && player.ActiveChild != this )
			return;

		CurrentTool?.OnFrame();

		UpdateToolgunPanel();

		// world model screen
		if ( SceneObject.IsValid() )
		{
			SceneObject.Batchable = false;
			SceneObject.Attributes.Set( "screenTexture", Texture );
		}

		// view model screen
		if ( ViewModelEntity.SceneObject.IsValid() )
		{
			ViewModelEntity.SceneObject.Batchable = false;
			ViewModelEntity.SceneObject.Attributes.Set( "screenTexture", Texture );
		}
	}

	public static void SetActiveTool( string toolId )
	{
		ConsoleSystem.Run( "tool_current", toolId );
		InventoryBar.SetActiveSlot( "weapon_tool" );
	}
}

namespace Sandbox.Tools
{
	public partial class BaseTool : BaseNetworkable
	{
		[Net]
		public Tool Parent { get; set; }

		[Net]
		public BasePlayer Owner { get; set; }

		protected virtual float MaxTraceDistance => 10000.0f;

		// Set this to override the [Library]'s class default
		public string Description { get; set; } = null;

		public virtual void Activate()
		{
			if ( Game.IsServer )
			{
				CreatePreviews();
				CurrentTool.CreateToolPanel();
			}
		}

		public virtual void CreateToolPanel()
		{

		}

		public virtual void Deactivate()
		{
			DeletePreviews();
			SpawnMenu.Instance?.ToolPanel?.DeleteChildren( true );
		}

		public virtual void Simulate()
		{

		}

		public virtual void BuildInput()
		{

		}

		public virtual void OnFrame()
		{
			UpdatePreviews();
		}

		public virtual void CreateHitEffects( Vector3 pos, Vector3 normal = new Vector3(), bool continuous = false )
		{
			Parent?.CreateHitEffects( pos, normal, continuous );
		}

		public virtual TraceResult DoTrace( bool checkCanTool = true ) {
			var tr = Owner.TraceRay( MaxTraceDistance );

			if ( checkCanTool && tr.Entity.IsValid() && !tr.Entity.IsWorld )
			{
				return CanToolParams.RunCanTool( Owner, ClassName, tr );
			}

			return tr;
		}

		protected string GetConvarValue( string name, string defaultValue = null )
		{
			return Game.IsServer
				? Owner.Client.GetClientData<string>( name, defaultValue )
				: ConsoleSystem.GetValue( name, default );
		}
	}

}
