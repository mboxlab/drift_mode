
namespace DM.Car;

using System;
using DM.UI;

public sealed class CarSelector : Component
{
	[Property] public List<GameObject> Cars { get; set; }
	public Action<GameObject> OnCarChanged { get; set; }
	public GameObject ActiveCar { get; set; }
	public BBox ActiveCarBounds { get; set; }
	public Vector3 ActiveCarCenter { get; set; }
	public int car { get; set; }

	private InteractiveCamera InteractiveCamera;

	protected override void OnStart()
	{
		base.OnStart();

		InteractiveCamera = Scene.GetAllComponents<InteractiveCamera>().First();

		ChangeCar( Cars.First() );
	}

	protected override void OnUpdate()
	{
		if ( !InteractiveCamera.IsLooking ) return;

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
		ActiveCar = car;
		ActiveCarBounds = car.GetBounds();
		ActiveCarCenter = ActiveCarBounds.Center;
		OnCarChanged.Invoke( car );
	}
}
