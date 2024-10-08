
using System.Text.Json.Serialization;
using Sandbox.UI;

namespace Sandbox.Car;

public sealed class CarSelector : Component
{
	[Property] public List<GameObject> Cars { get; set; }

	public event Action<GameObject> OnCarChanged;
	public GameObject ActiveCar { get; private set; }
	public int CarIndex { get; private set; }
	public string SavePath { get; private set; } = "ActiveCar.json";

	protected override void OnStart()
	{
		base.OnStart();
		LoadCar();
	}

	protected override void OnUpdate()
	{

		if ( InteractiveCamera.IsOnOrigin && Input.Pressed( "Left" ) )
		{
			CarIndex = (CarIndex + 1) % Cars.Count;
			if ( ActiveCar.IsValid() )
			{
				ChangeCar( Cars[CarIndex] );
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
