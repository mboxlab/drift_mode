using System.Text.Json.Serialization;

namespace Sandbox.Car;

[Icon( "checkroom" )]
public sealed class CarDresser : Component, Component.INetworkListener, ICarDresserEvent
{
	[Property] public List<Part> Parts = new();

	public ModelRenderer CarRenderer;
	public static string SavePath { get; set; } = "dress.json";

	public Dictionary<string, int> Default
	{
		get
		{
			Dictionary<string, int> models = new();
			Parts.ForEach( part => models[part.Name] = 0 );
			return models;
		}
	}

	public Dictionary<string, int> Current
	{
		get
		{
			Dictionary<string, int> models = new();
			Parts.ForEach( part => models[part.Name] = part.Index );
			return models;
		}
	}

	private Dictionary<string, int> Get()
	{
		if ( !FileSystem.OrganizationData.FileExists( SavePath ) ) return Default;

		CarDresses dresses = FileSystem.OrganizationData.ReadJson<CarDresses>( SavePath );
		Dictionary<string, int> json = dresses.Cars.GetValueOrDefault( CarRenderer.Model.Name );
		if ( json is not null ) return json;

		return Default;
	}

	void ICarDresserEvent.OnSave( Part part )
	{
		bool exists = FileSystem.OrganizationData.FileExists( SavePath );
		CarDresses dresses = exists ? FileSystem.OrganizationData.ReadJson<CarDresses>( SavePath ) : new();

		dresses.Cars = dresses.Cars is null ? new() : dresses.Cars;
		dresses.Cars[CarRenderer.Model.Name] = Current;

		FileSystem.OrganizationData.WriteJson( SavePath, dresses );
		part.TuningOption.Apply();

		ICarDresserEvent.Post( x => x.OnPartChanged( part ) );
	}

	public void Dress() => Dress( Get() );
	public void Dress( Dictionary<string, int> models )
	{
		Parts.ForEach( part => part.Dress( models[part.Name] ) );
	}

	public void OnNetworkSpawn( Connection connection )
	{
		//Dress();
	}

	protected override void OnStart()
	{
		base.OnStart();

		Parts = Components.GetAll<Part>( FindMode.InDescendants ).ToList();
		CarRenderer = Components.Get<ModelRenderer>();

		Dress();

		ICarDresserEvent.Post( x => x.OnLoad( Parts ) );
	}
}

public struct CarDresses
{
	[JsonInclude] public Dictionary<string, Dictionary<string, int>> Cars;
}

public interface ICarDresserEvent : ISceneEvent<ICarDresserEvent>
{

	void OnSave( Part part ) { }
	void OnPartChanged( Part part ) { }
	void OnLoad( List<Part> parts ) { }
}
