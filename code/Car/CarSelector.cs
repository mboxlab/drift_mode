
using System.Text.Json.Serialization;
using Sandbox.UI;

namespace Sandbox.Car;

public sealed class CarSelector : Component
{
	[Property] public List<GameObject> Cars { get; set; }

	public event Action<GameObject> OnCarChanged;
	public GameObject ActiveCar { get; private set; }
	[Property]
	public int CarIndex { get; private set; }
	public static string SavePath { get; private set; } = "ActiveCar.json";

	protected override void OnStart()
	{
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
		GameObject car = newCar.Clone();
		car.Name = newCar.Name;
		car.WorldPosition = WorldPosition;

		car.GetComponent<CameraController>().Enabled = false;
		car.GetComponent<CarController>().Enabled = false;
		car.GetComponent<Rigidbody>().Locking = new PhysicsLock() { Pitch = true, Yaw = true, Roll = true };
		ActiveCar = car;
		CarIndex = Cars.IndexOf( newCar );

		OnCarChanged?.Invoke( car );
		SaveCar();
	}

	private void SaveCar()
	{
		CarJson dresses = new()
		{
			CarIndex = CarIndex,
			CarPrefabSource = ActiveCar.PrefabInstanceSource,
			CarName = ActiveCar.Name
		};

		FileSystem.Data.WriteJson( SavePath, dresses );
	}
	private void LoadCar()
	{
		bool exists = FileSystem.Data.FileExists( SavePath );
		if ( !exists )
		{
			ChangeCar( Cars.First() );
			return;
		}

		CarJson dresses = FileSystem.Data.ReadJson<CarJson>( SavePath );

		if ( dresses.CarPrefabSource is null )
		{
			ChangeCar( Cars.First() );
			return;
		}

		ChangeCar( Cars.Find( ( car ) => car.Name == dresses.CarName ) );
	}


	public struct CarJson
	{
		[JsonInclude] public int CarIndex { get; set; }
		[JsonInclude] public string CarPrefabSource { get; set; }
		[JsonInclude] public string CarName { get; set; }
	}
}
