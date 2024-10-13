using System.Text.Json.Serialization;
using Sandbox.Car;
using Sandbox.Tuning;

namespace Sandbox.Utils;


public static class CarSaver
{
	public static readonly string CarSaveFolder = "cars";
	public static readonly string ActiveCarSavePath = "ActiveCar.json";
	public static bool CanSave() => CarSelector.ActiveCar.IsValid();
	public static void SaveActiveCar() => SaveCar( CarSelector.ActiveCar.GetComponent<CarController>() );
	public static string GetSavePath( CarController car ) => GetSavePath( car.Name );
	public static string GetSavePath( string name )
	{
		FileSystem.Data.CreateDirectory( CarSaveFolder );
		return $"{CarSaveFolder}/{name}.json";
	}
	public static void SetActiveCar( string name )
	{
		FileSystem.Data.WriteJson( ActiveCarSavePath, name );
	}

	public static void SaveCar( CarController car )
	{
		FileSystem.Data.WriteAllText( GetSavePath( car ), car.TuningContainer.Serialize() );
		ICarSaverEvent.Post( x => x.OnSave() );
	}
	public static string GetActiveCarName()
	{
		return FileSystem.Data.FileExists( ActiveCarSavePath ) ? FileSystem.Data.ReadJson<string>( ActiveCarSavePath ) : null;
	}
	public static GameObject LoadActiveCar()
	{
		return LoadCar( GetActiveCarName() + ".json" );
	}

	public static GameObject LoadCar( string path )
	{
		var prefab = ResourceLibrary.Get<PrefabFile>( $"prefabs/cars/{path[..^5]}.prefab" );
		var carPrefab = SceneUtility.GetPrefabScene( prefab );
		GameObject car = carPrefab.Clone();
		CarController controller = car.GetComponent<CarController>();
		ICarSaverEvent.Post( x => x.OnPreLoad( controller ) );

		bool exists = FileSystem.Data.FileExists( path );

		ICarSaverEvent.Post( x => x.OnLoad( controller ) );

		return car;
	}
	public static GameObject LoadFakeCar( string path )
	{
		return SetCarIsFake( LoadCar( path ) );
	}
	public static GameObject SetCarIsFake( GameObject car )
	{

		car.GetComponent<Rigidbody>().Locking = new PhysicsLock() { Pitch = true, Yaw = true, Roll = true, X = true, Y = true };
		car.GetComponent<CameraController>().Destroy();

		CarController controller = car.GetComponent<CarController>();
		controller.Disable();

		return car;
	}
	public static CarData? GetJsonByName( string carName )
	{
		string path = carName + ".json";
		bool exists = FileSystem.Data.FileExists( path );
		if ( !exists )
			return null;

		return FileSystem.Data.ReadJson<CarData>( path );
	}

}
public interface ICarSaverEvent : ISceneEvent<ICarSaverEvent>
{
	void OnSave() { }
	void OnPreLoad( CarController car ) { }
	void OnLoad( CarController car ) { }
}

public struct CarData
{
	[JsonInclude] public string CarName { get; set; }
	[JsonInclude] public TuningContainer Tunings { get; set; }
}
