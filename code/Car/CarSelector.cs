
namespace DM.Car;

public sealed class CarSelector : Component
{
	[Property] public List<GameObject> Cars { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		var car = Cars.First();

		car = car.Clone();
		car.Transform.Position = Transform.Position;
		car.Components.Get<Rigidbody>().MotionEnabled = false;
		car.Components.Get<Car>().Enabled = false;
	}

}
