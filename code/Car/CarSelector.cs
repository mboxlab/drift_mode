
namespace DM.Car;

public sealed class CarSelector : Component
{
	[Property] public GameObject ActiveCar { get; set; }
	[Property] public List<GameObject> Cars { get; set; }
	[Property] public int car { get; set; }

	protected override void OnStart()
	{
		base.OnStart();
		ChangeCar( Cars.First() );
	}

	protected override void OnUpdate()
	{
		if ( Input.Pressed( "Left" ) )
		{
			car = (car + 1) % Cars.Count;
			if ( ActiveCar.IsValid() )
			{
				ChangeCar( Cars[car] );
			}
		}
	}

	private void ChangeCar( GameObject newCar )
	{
		ActiveCar?.Destroy();
		GameObject car = newCar.Clone();
		car.Name = newCar.Name;
		car.Transform.Position = Transform.Position;
		car.Components.Get<Rigidbody>().MotionEnabled = false;
		car.Components.Get<Car>().Enabled = false;
		car.Components.Get<CameraController>().Destroy();
		ActiveCar = car;
	}
}
