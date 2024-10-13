
using Sandbox.UI;
using Sandbox.Utils;

namespace Sandbox.Car;

public sealed class CarSelector : Component
{
	[Property] public List<GameObject> Cars { get; set; }

	[Property] public Action<GameObject> OnCarChanged;
	public static GameObject ActiveCar { get; private set; }
	public static CarSelector Instance { get; private set; }
	[Property]
	public int CarIndex { get; private set; }
	protected override void OnStart()
	{
		Instance = this;
		base.OnStart();
		LoadCar();
	}

	protected override void OnUpdate()
	{
		if ( !InteractiveCamera.IsOnOrigin )
			return;

		int index = (Input.Pressed( "Right" ) ? 1 : 0) - (Input.Pressed( "Left" ) ? 1 : 0);
		if ( index != 0 )
		{

			CarIndex = (CarIndex + index) % Cars.Count;
			if ( CarIndex == -1 )
				CarIndex = Cars.Count - 1;
			if ( ActiveCar.IsValid() )
				ChangeCar( Cars[CarIndex] );
		}
	}

	private void ChangeCar( GameObject newCar )
	{
		ActiveCar?.Destroy();
		SetCar( newCar.Name );
		OnCarChanged?.Invoke( ActiveCar );
		CarSaver.SetActiveCar( ActiveCar.Name );
	}

	private void SetCar( GameObject car ) => SetupCar( car );
	private void SetCar( string carName ) => SetupCar( CarSaver.LoadFakeCar( carName + ".json" ) );

	private void SetupCar( GameObject car )
	{
		car.WorldPosition = WorldPosition;
		car.WorldRotation = WorldRotation;
		car.GetComponent<CarController>().ClientInit();

		ActiveCar = car;
		CarIndex = Cars.FindIndex( x => x.Name == car.Name );
	}
	private void LoadCar()
	{

		if ( CarSaver.GetActiveCarName() != null )
			SetCar( CarSaver.GetActiveCarName() );
		else
		{
			SetCar( Cars.First().Name );
			CarSaver.SetActiveCar( Cars.First().Name );
			CarSaver.SaveActiveCar();
		}
	}
}
