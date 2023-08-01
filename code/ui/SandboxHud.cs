using System;
using Sandbox;
using Sandbox.UI;

[Library]
public partial class SandboxHud : HudEntity<RootPanel>
{
	public static SandboxHud Instance;

	public static event Action OnHudLoaded;
	public SandboxHud()
	{
		if ( !Game.IsClient )
			return;
		Instance = this;

		RootPanel.StyleSheet.Load( "/Styles/sandbox.scss" );

		RootPanel.AddChild<Chat>();
		RootPanel.AddChild<VoiceList>();
		RootPanel.AddChild<VoiceSpeaker>();
		RootPanel.AddChild<KillFeed>();
		RootPanel.AddChild<Scoreboard<ScoreboardEntry>>();
		RootPanel.AddChild<Health>();
		RootPanel.AddChild<InventoryBar>();
		RootPanel.AddChild<CurrentTool>();
		RootPanel.AddChild<SpawnMenu>();
		RootPanel.AddChild<Crosshair>();
		OnHudLoaded?.Invoke();
		HotReloadTool();
	}

	[ClientRpc]
	public static void HotReloadTool()
	{
		CurrentTool.GetCurrentTool()?.Activate();
	}
}
