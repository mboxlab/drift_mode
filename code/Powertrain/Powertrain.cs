
using Sandbox.Car;

namespace Sandbox.Powertrain;

public partial class Powertrain
{
	[Property] public ClutchComponent Clutch { get; set; } = new();
	//public List<DifferentialComponent> Differentials = new List<DifferentialComponent>();
	[Property] public EngineComponent Engine { get; set; } = new();
	[Property] public TransmissionComponent Transmission { get; set; } = new();
	//public List<WheelGroup> wheelGroups = new List<WheelGroup>();
	[Property] public List<WheelComponent> Wheels { get; set; } = new();


	public int WheelCount
	{
		get { return Wheels.Count; }
	}
}
