﻿using System.Text.Json.Serialization;

namespace Sandbox.Car;

[Icon( "checkroom" )]
public sealed class CarDresser : Component, Component.INetworkListener, ICarDresserEvent
{
	[Property] public List<Part> Parts = new();

	public ModelRenderer CarRenderer;

	private const string Path = "dress.json";

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
		if ( !FileSystem.OrganizationData.FileExists( Path ) ) return Default;

		CarDresses dresses = FileSystem.OrganizationData.ReadJson<CarDresses>( Path );
		Dictionary<string, int> json = dresses.Cars.GetValueOrDefault( CarRenderer.Model.Name );
		if ( json is not null ) return json;

		return Default;
	}

	void ICarDresserEvent.Save() => Save();
	private void Save()
	{
		bool exists = FileSystem.OrganizationData.FileExists( Path );
		CarDresses dresses = exists ? FileSystem.OrganizationData.ReadJson<CarDresses>( Path ) : new();

		dresses.Cars = dresses.Cars is null ? new() : dresses.Cars;
		dresses.Cars[CarRenderer.Model.Name] = Current;

		FileSystem.OrganizationData.WriteJson( Path, dresses );
	}

	public void Dress() => Dress( Get() );
	public void Dress( Dictionary<string, int> models )
	{
		Parts.ForEach( part => part.Index = models[part.Name] );
		Save();
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
	}
}

public struct CarDresses
{
	[JsonInclude] public Dictionary<string, Dictionary<string, int>> Cars;
}

public interface ICarDresserEvent : ISceneEvent<ICarDresserEvent>
{
	void Save() { }
}
