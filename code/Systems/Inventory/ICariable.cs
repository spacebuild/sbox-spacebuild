namespace Sandbox.Systems.Inventory;

public interface ICariable: IEntity {
	
	CariableHoldTypes HoldType { get; }
	CariableHandedness Handedness { get; }
	
	float AimBodyWeight { get; }
	
	bool CanDrop( Entity owner );

	void OnDrop( Entity owner );

	bool CanCarry( Entity owner );

	void OnCarry( Entity owner );
	
	
	// Entity methods 

	void Simulate( IClient cl );
	void BuildInput();
}
