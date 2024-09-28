
using System;
using Sandbox.Car;

[Category( "Vehicles" )]
public class WheelManager : Component
{
	private float combinedLoad;
	private int _wheelCount;

	[Property] public float CombinedLoad { get => combinedLoad; }
	[Property] public List<WheelCollider> Wheels = new();

	protected override void OnStart()
	{	
		Wheels = Components.GetAll<WheelCollider>( FindMode.InDescendants ).ToList();
		_wheelCount = Wheels.Count;
	}

	protected override void OnFixedUpdate()
	{
		combinedLoad = 0f;
		for ( int i = 0; i < _wheelCount; i++ )
		{
			WheelCollider wheel = Wheels[i];
			combinedLoad += wheel.Load;
		}
	}

	public void Register( WheelCollider wheel )
	{
		if ( !Wheels.Contains( wheel ) )
		{
			Wheels.Add( wheel );
			_wheelCount++;
		}
	}


	public void UnRegister( WheelCollider wheel )
	{
		if ( Wheels.Contains( wheel ) )
		{
			Wheels.Remove( wheel );
			_wheelCount--;
		}
	}



}
