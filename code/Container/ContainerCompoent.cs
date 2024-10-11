
using Sandbox.Car;

namespace Sandbox.Container;

// TODO
public sealed class ContainerCompoent : Component
{
	[Property] public SkinnedModelRenderer ContainerRenderer { get; set; }
	[Property] public GameObject CarTransform { get; set; }
	[Property] public List<GameObject> Cars { get; set; }
	protected override void OnStart()
	{
		base.OnStart();

		var car = Cars[Game.Random.Int( 0, Cars.Count - 1 )].Clone( CarTransform.WorldTransform );
		CarSelector.FakeCar( car );
	}
}
