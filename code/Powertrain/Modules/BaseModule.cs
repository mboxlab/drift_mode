using Sandbox.Car;

namespace Sandbox.Powertrain.Modules;


[Category( "Vehicle Module" )]
public abstract class BaseModule : Component
{
	[Property, Group( "Components" )] public CarController CarController;
}
