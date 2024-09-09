using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DM.Vehicle;

public class WheelControllerManager : Component
{
	[NonSerialized] public float combinedLoad;

	[Property] public List<WheelAPI> Wheels { get; } = new();
	private int _wheelCount = 0;

	protected override void OnFixedUpdate()
	{
		UpdateCombinedLoad();
	}

	private void UpdateCombinedLoad()
	{
		combinedLoad = 0f;
		for ( int i = 0; i < _wheelCount; i++ )
		{
			WheelAPI wheel = Wheels[i];
			combinedLoad += wheel.Load;
		}
	}

	public void Register( WheelAPI wheel )
	{
		if ( !Wheels.Contains( wheel ) )
		{
			Wheels.Add( wheel );
			_wheelCount++;
		}
	}

	public void Deregister( WheelAPI wheel )
	{
		if ( Wheels.Contains( wheel ) )
		{
			Wheels.Remove( wheel );
			_wheelCount--;
		}
	}

}
