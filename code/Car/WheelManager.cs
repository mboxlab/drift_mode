namespace DM.Car;

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
		UpdateCombinedLoad();
	}


	private void UpdateCombinedLoad()
	{
		combinedLoad = 0f;
		for ( int i = 0; i < _wheelCount; i++ )
		{
			Wheel wheel = Wheels[i];
			combinedLoad += wheel.Load;
		}
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
