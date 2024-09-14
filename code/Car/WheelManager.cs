using DM.Ground;
using System;

namespace DM.Car;

[Category( "Vehicles" )]
public class WheelManager : Component
{
	private float combinedLoad;
	private int _wheelCount;

	[Property] public float CombinedLoad { get => combinedLoad; }
	[Property] public List<Wheel> Wheels = new();

	protected override void OnStart()
	{
		Wheels = Components.GetAll<Wheel>( FindMode.InDescendants ).ToList();
		_wheelCount = Wheels.Count;
	}

	protected override void OnFixedUpdate()
	{
		combinedLoad = 0f;
		for ( int i = 0; i < _wheelCount; i++ )
		{
			Wheel wheel = Wheels[i];
			combinedLoad += wheel.Load;
			SetupPreset( wheel );
		}
	}
	private static void SetupPreset( Wheel wheel )
	{
		Enum.TryParse( wheel.groundHit.Surface.ResourceName, true, out FrictionPreset.PresetsEnum presetName );
		var newPreset = FrictionPreset.Presets[presetName];
		if ( newPreset is not null )
			wheel.FrictionPreset = FrictionPreset.Presets[presetName];
		else
			wheel.FrictionPreset = FrictionPreset.Asphalt;
	}

	public void Register( Wheel wheel )
	{
		if ( !Wheels.Contains( wheel ) )
		{
			Wheels.Add( wheel );
			_wheelCount++;
		}
	}


	public void Deregister( Wheel wheel )
	{
		if ( Wheels.Contains( wheel ) )
		{
			Wheels.Remove( wheel );
			_wheelCount--;
		}
	}



}
