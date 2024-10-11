
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
		CarSaver.SaveActiveCar();
		ActiveCar?.Destroy();
		SetCar( newCar );
		OnCarChanged?.Invoke( ActiveCar );
		CarSaver.SetActiveCar( ActiveCar );
	}

	private void SetCar( GameObject newCar )
	{
		GameObject car = newCar.Clone();
		car.Name = newCar.Name;
		car.WorldPosition = WorldPosition;
		CarController controller = CarSaver.FakeCar( car );

		controller.ClientInit();

		ActiveCar = car;
		CarIndex = Cars.IndexOf( newCar );
	}

	private void LoadCar()
	{
		GameObject car = CarSaver.LoadActiveCar();
		if ( car != null )
			SetCar( car );
		else
			SetCar( Cars.First() );
	}
}
