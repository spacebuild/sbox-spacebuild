using Sandbox.Internal;

namespace Sandbox.Systems.Camera; 

public interface IPlayerCamera: IComponent, INetworkTable {

	void Update( );

	void Remove();

}
