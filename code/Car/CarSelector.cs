
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

	public static CarJson? GetJsonByName( string carName )
	{
		string path = carName + ".json";
		bool exists = FileSystem.Data.FileExists( path );
		if ( !exists )
			return null;

		return FileSystem.Data.ReadJson<CarJson>( path );
	}

	private void ChangeCar( GameObject newCar )
	{
		ActiveCar?.Destroy();
		GameObject car = newCar.Clone();
		car.Name = newCar.Name;
		car.WorldPosition = WorldPosition;

		CarController controller = FakeCar( car );

		controller.OnCarPropertiesChanged += SaveCar;

		ActiveCar = car;
		CarIndex = Cars.IndexOf( newCar );

		OnCarChanged?.Invoke( car );
		SaveCar();
	}
	public static CarController FakeCar( GameObject car )
	{
		car.GetComponent<Rigidbody>().Locking = new PhysicsLock() { Pitch = true, Yaw = true, Roll = true, X = true, Y = true };
		car.GetComponent<CameraController>().Destroy();

		CarController controller = car.GetComponent<CarController>();
		controller.Enabled = false;

		CarJson? config = GetJsonByName( car.Name );
		if ( config.HasValue )
		{
			controller.WheelRadius = config.Value.WheelRadius;
			controller.WheelWidth = config.Value.WheelWidth;
		}

		controller.UpdateCarProperties();
		return controller;
	}
	private void SaveCar()
	{
		CarJson carJson = new()
		{
			CarIndex = CarIndex,
			CarPrefabSource = ActiveCar.PrefabInstanceSource,
			CarName = ActiveCar.Name,
			WheelRadius = ActiveCar.GetComponentInChildren<CarController>( true ).WheelRadius,
		};

		FileSystem.Data.WriteJson( ActiveCar.Name + ".json", carJson );
		FileSystem.Data.WriteJson( SavePath, carJson );
	}

	private void LoadCar() => LoadCar( SavePath );
	private void LoadCar( string path )
	{
		bool exists = FileSystem.Data.FileExists( path );
		if ( !exists )
		{
			ChangeCar( Cars.First() );
			return;
		}

		CarJson carJson = FileSystem.Data.ReadJson<CarJson>( path );

		if ( carJson.CarPrefabSource is null )
		{
			ChangeCar( Cars.First() );
			return;
		}

		ChangeCar( Cars.Find( ( car ) => car.Name == carJson.CarName ) );
	}

	public struct CarJson
	{
		[JsonInclude] public int CarIndex { get; set; }
		[JsonInclude] public string CarPrefabSource { get; set; }
		[JsonInclude] public string CarName { get; set; }
		[JsonInclude] public float WheelRadius { get; set; }
		[JsonInclude] public float WheelWidth { get; set; }
	}
}
