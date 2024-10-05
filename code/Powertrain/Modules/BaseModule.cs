using Sandbox.Car;

namespace Sandbox.Powertrain.Modules;

public abstract class BaseModule : Component
{
	[Property, Group( "Components" )] public CarController CarController;
}
