using Sandbox.Systems.Player;

namespace Sandbox.Systems.Inventory;

public partial class BasePlayerInventory: EntityComponent<Entity>, ISingletonComponent
{
	[Net] public IList<ICariable> Items { get; set; }
	[Net, Predicted] public ICariable ActiveCariable { get; set; }

	public bool Add( ICariable cariable, bool makeActive = true ) {
		if ( Items.Contains( cariable ) ) {
			return false;
		}
		Items.Add(cariable);
		if ( makeActive ) {
			SetActive( cariable );
		}
		return true;
	}

	public bool Remove( ICariable cariable, bool drop = false ) {
		var success = Items.Remove( cariable );
		if ( success && drop ) {
			// TODO - Drop the item on the ground
		}

		return success;
	}

	public void SetActive( ICariable cariable ) {
		var currentCariable = ActiveCariable;
		var owner = Entity;
		if ( currentCariable.IsValid( ) ) { //TODO difference between IsValid and IsValid() ??
			if ( !currentCariable.CanCarry( owner ) ) {
				return;
			}
			currentCariable.OnDrop(owner);
			ActiveCariable = null;
		}

		if ( !cariable.CanCarry( owner ) ) {
			return;
		}

		ActiveCariable = cariable;
		cariable?.OnCarry( owner );
	}
	
	protected override void OnDeactivate()
	{
		if ( Game.IsServer )
		{
			Items.ToList().ForEach( x => x.Delete() );
		}
	}
	
	public ICariable GetSlot( int slot )
	{
		return Items.ElementAtOrDefault( slot ) ?? null;
	}
	
	private int GetSlotIndexFromInput( string slot )
	{
		return slot switch
		{
			"slot1" => 0,
			"slot2" => 1,
			"slot3" => 2,
			"slot4" => 3,
			"slot5" => 4,
			_ => -1
		};
	}
	
	private void TrySlotFromInput( string slot ) {
		if ( Entity is not BasePlayer player ) {
			return;
		}
		if ( !Input.Pressed( slot ) ) {
			return;
		}

		Input.ReleaseAction( slot );

		if ( GetSlot( GetSlotIndexFromInput( slot ) ) is { } cariable )
		{
			player.ActiveCariableInput = cariable;
		}
	}
	
	public void BuildInput()
	{
		TrySlotFromInput( "slot1" );
		TrySlotFromInput( "slot2" );
		TrySlotFromInput( "slot3" );
		TrySlotFromInput( "slot4" );
		TrySlotFromInput( "slot5" );

		ActiveCariable?.BuildInput();
	}
	
	public void Simulate( IClient cl )
	{
		if ( Entity is not BasePlayer player ) {
			return;
		}
		if ( player.ActiveCariableInput != null && ActiveCariable != player.ActiveCariableInput )
		{
			SetActive( player.ActiveCariableInput );
			player.ActiveCariableInput = null;
		}

		ActiveCariable?.Simulate( cl );
	}
	
}
