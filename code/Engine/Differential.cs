
using DM.Car;
using DM.Engine.Gearbox;

namespace DM.Engine;
[Category( "Vehicles" )]
public class Differential : Component
{
	[Property, ReadOnly] public float AverageRPM { get; set; } = 0;
	[Property] public float FinalDrive { get; set; } = 2.2f;
	[Property] public float DistributionCoeff { get; set; } = 1f;
	[Property] public BaseGearbox Gearbox { get; set; }
	[Property] public Wheel LeftWheel { get; set; }
	[Property] public Wheel RightWheel { get; set; }
	[Property, ReadOnly] public float LinearVelocity { get => GetVel(); }

	private float GetVel()
	{
		float lwav = LeftWheel.AngularVelocity;
		float rwav = LeftWheel.AngularVelocity;
		return (lwav + rwav) / 2;
	}
	protected override void OnFixedUpdate()
	{

		float lwav = LeftWheel.AngularVelocity;
		float rwav = LeftWheel.AngularVelocity;

		float inertia = LeftWheel.Inertia + RightWheel.Inertia;
		float simmetric = Gearbox.Torque * DistributionCoeff * FinalDrive;
		float _lock = (lwav - rwav) / 2 * inertia * Time.Delta;

		LeftWheel.ApplyMotorTorque( simmetric );
		RightWheel.ApplyMotorTorque( simmetric );

		lwav = LeftWheel.AngularVelocity;
		rwav = LeftWheel.AngularVelocity;

		AverageRPM = (lwav + rwav) / 2 * Engine.RAD_TO_RPM * FinalDrive;
	}

}
