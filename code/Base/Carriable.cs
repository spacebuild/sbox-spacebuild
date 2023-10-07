using Sandbox;
using Sandbox.Systems.Inventory;
using BaseCarriable = Sandbox.Base.BaseCarriable;

public partial class Carriable : BaseCarriable, IUse
{
	public override void CreateViewModel()
	{
		Game.AssertClient();

		if ( string.IsNullOrEmpty( ViewModelPath ) )
			return;

		ViewModelEntity = new ViewModel
		{
			Position = Position,
			Owner = Owner,
			EnableViewmodelRendering = true
		};

		ViewModelEntity.SetModel( ViewModelPath );
	}

	public bool OnUse( Entity user )
	{
		return false;
	}

	public virtual bool IsUsable( Entity user )
	{
		return Owner == null;
	}

	public virtual bool CanUnequip( Entity owner ) {
		return true;
	}

	public void OnUnEquip( Entity owner ) {
		OnDrop(owner);
	}

	public bool CanEquip( Entity owner ) {
		return CanCarry( owner );
	}

	public void OnEquip( Entity owner ) {
		OnCarry( owner );
	}
}
