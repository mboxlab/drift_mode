
using Sandbox.Tuning;
using Sandbox.Utils;
using Sandbox.VR;

namespace Sandbox.Car;

[Icon( "checkroom" )]
public sealed class CarDresser : Component, Component.INetworkListener
{
	[Property] public CarController CarController { get; private set; }
	[Property] public List<CarTuning> Defaults { get; private set; }
	protected override void OnStart()
	{
		base.OnStart();
		CarController ??= Components.Get<CarController>( FindMode.EverythingInSelfAndAncestors );
		CarController.TuningContainer.Deserialize( FileSystem.Data.ReadAllText( CarSaver.GetSavePath( CarController ) ) );

		foreach ( var item in Defaults )
			CarController.TuningContainer.TryAdd( item );

		CarController.TuningContainer.Apply( CarController.BodyRenderer );

		ICarDresserEvent.Post( x => x.PostLoad( CarController ) );
		CarSaver.SaveCar( CarController );

	}
}
public interface ICarDresserEvent : ISceneEvent<ICarDresserEvent>
{
	void PostLoad( CarController controller );
}
