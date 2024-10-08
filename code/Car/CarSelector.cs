
using Sandbox.UI;

namespace Sandbox.Car;

public sealed class CarSelector : Component
{
	[Property] public List<GameObject> Cars { get; set; }
	public Action<GameObject> OnCarChanged { get; set; }
	public static GameObject ActiveCar { get; set; }
	public BBox ActiveCarBounds { get; set; }
	public Vector3 ActiveCarCenter { get; set; }
	public int car { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		ChangeCar( Cars.First() );
	}

	protected override void OnUpdate()
	{

		if ( InteractiveCamera.IsOnOrigin && Input.Pressed( "Left" ) )
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
		car.WorldPosition = WorldPosition;
		ActiveCar = car;
		ActiveCarBounds = car.GetBounds();
		ActiveCarCenter = ActiveCarBounds.Center;
		//ActiveCar.Components.Get<InteractiveObject>().LocalPosition = new Vector3( 0, 0, ActiveCarBounds.Center.z );
		OnCarChanged?.Invoke( car );
	}
}
