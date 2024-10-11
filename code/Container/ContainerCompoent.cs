
namespace Sandbox.Container;

public sealed class ContainerCompoent : Component
{
	[Property] public SkinnedModelRenderer ContainerRenderer { get; set; }
	protected override void OnStart()
	{
		base.OnStart();
	}
}
