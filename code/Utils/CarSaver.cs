using System.Text.Json.Serialization;
using Sandbox.Car;
using Sandbox.Tuning;

namespace Sandbox.Utils;


public static class CarSaver
{
	public static readonly string ActiveCarSavePath = "ActiveCar.json";
	public static bool CanSave() => CarSelector.ActiveCar.IsValid();
	public static void SaveActiveCar() => SaveCar( CarSelector.ActiveCar );
	private static CarJson GetCarJson( GameObject car )
	{

		Dictionary<string, TuningOption> tunings = new();
		ITuningEvent.Post( x =>
		{
			TuningOption tuning = x.GetOpiton();
			tunings.Add( tuning.Name, tuning );
		} );

		CarJson carJson = new()
		{
			CarPrefabSource = car.PrefabInstanceSource,
			CarName = car.Name,
			Tunings = tunings,
		};
		return carJson;
	}

	public static void SetActiveCar( GameObject car )
	{
		CarJson carJson = GetCarJson( car );

		FileSystem.Data.WriteJson( ActiveCarSavePath, carJson );
	}

	public static void SaveCar( GameObject car )
	{
		CarJson carJson = GetCarJson( car );

		FileSystem.Data.WriteJson( car.Name + ".json", carJson );

		ICarSaverEvent.Post( x => x.OnSave() );
	}

	public static GameObject LoadActiveCar() => LoadCar( ActiveCarSavePath );

	public static GameObject LoadCar( string path )
	{
		bool exists = FileSystem.Data.FileExists( path );
		if ( !exists )
			return default;

		CarJson carJson = FileSystem.Data.ReadJson<CarJson>( path );

		var prefab = ResourceLibrary.Get<PrefabFile>( carJson.CarPrefabSource );
		var player = SceneUtility.GetPrefabScene( prefab );
		ICarSaverEvent.Post( x => x.OnLoad() );

		return player;
	}
	public static CarController FakeCar( GameObject car )
	{
		car.GetComponent<Rigidbody>().Locking = new PhysicsLock() { Pitch = true, Yaw = true, Roll = true, X = true, Y = true };
		car.GetComponent<CameraController>().Destroy();

		CarController controller = car.GetComponent<CarController>();
		controller.Enabled = false;

		CarJson? config = GetJsonByName( car.Name );
		if ( config.HasValue )
			controller.ApplyTunings( config.Value.Tunings );

		return controller;
	}
	public static CarJson? GetJsonByName( string carName )
	{
		string path = carName + ".json";
		bool exists = FileSystem.Data.FileExists( path );
		if ( !exists )
			return null;

		return FileSystem.Data.ReadJson<CarJson>( path );
	}

}
public interface ICarSaverEvent : ISceneEvent<ICarSaverEvent>
{
	void OnSave() { }
	void OnLoad() { }
}

public struct CarJson
{
	[JsonInclude] public string CarPrefabSource { get; set; }
	[JsonInclude] public string CarName { get; set; }
	[JsonInclude] public Dictionary<string, TuningOption> Tunings { get; set; }
}
