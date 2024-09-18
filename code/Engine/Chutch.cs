
using DM.Engine.Gearbox;

namespace DM.Engine;
[Category( "Vehicles" )]
public class Clutch : Component
{
	private bool clutching => Input.Down( "Clutch" );

	[Property, ReadOnly] public float TargetTorque { get; set; } = 0;
	[Property, ReadOnly] public float Torque { get; set; } = 0;
	[Property] public float Stiffness { get; set; } = 1;
	[Property] public float Damping { get; set; } = 1;
	[Property] public EngineICE Engine { get; set; }
	[Property] public BaseGearbox Gearbox { get; set; }
	[Property] public bool Clutching { get => clutching; }
	internal void Think()
	{

		float engineRPM = Engine.RPM;
		Gearbox.Think();
		float gearboxRPM = Gearbox.RPM;

		int gearboxRatioNotZero = Gearbox.Ratio != 0 ? 1 : 0;

		float slip = ((engineRPM - gearboxRPM) * EngineICE.RPM_TO_RAD) * gearboxRatioNotZero;

		float tslip = (Engine.Torque) * gearboxRatioNotZero;

		TargetTorque = (tslip + slip * Stiffness) * (1 - (Clutching ? 1 : 0));

		Torque = MathX.Lerp( Torque, TargetTorque, Damping );

	}

}
