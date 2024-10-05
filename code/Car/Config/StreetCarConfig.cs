
namespace Sandbox.Car.Config;

public class StreetCarConfig : ICarConfig
{
	public override DriveType DriveType { get; set; } = DriveType.RWD;
	public override bool AutomaticGearBox { get; set; } = true;
	public override float MaxMotorTorque { get; set; } = 300;
	public override float MaxRPM { get; set; } = 7000;
	public override float MinRPM { get; set; } = 700;
	public override float CutOffRPM { get; set; } = 6800;
	public override float CutOffOffsetRPM { get; set; } = 500;
	public override float CutOffTime { get; set; } = 0.1f;
	public override float ProbabilityBackfire { get; set; } = 0.2f;
	public override float RpmToNextGear { get; set; } = 6500f;
	public override float RpmToPrevGear { get; set; } = 4500f;
	public override float MaxForwardSlipToBlockChangeGear { get; set; } = 0.5f;
	public override float RpmEngineToRpmWheelsLerpSpeed { get; set; } = 15f;
	public override float[] GearsRatio { get; set; } = new float[5] { 3.59f, 2.02f, 1.38f, 1f, 0.87f };
	public override float MainRatio { get; set; } = 4.3f;
	public override float ReversGearRatio { get; set; } = 4f;

	public override float MaxBrakeTorque { get; set; } = 3000;
	public override float TargetSpeedIfBrakingGround { get; set; } = 20f;
	public override bool EnableSteerAngleMultiplier { get; set; } = true;
	public override float MinSteerAngleMultiplier { get; set; } = 0.05f;
	public override float MaxSteerAngleMultiplier { get; set; } = 1f;
	public override float MaxSpeedForMinAngleMultiplier { get; set; } = 250;

	public override float MaxSteerAngle { get; set; } = 25f;
	public override float SteerAngleChangeSpeed { get; set; } = 5f;
	public override float MinSpeedForSteerHelp { get; set; } = 20f;
	public override float HelpSteerPower { get; set; } = 0.8f;
	public override float OppositeAngularVelocityHelpPower { get; set; } = 0.03f;
	public override float PositiveAngularVelocityHelpPower { get; set; } = 0.03f;
	public override float MaxAngularVelocityHelpAngle { get; set; } = 90f;
	public override float AngularVelucityInMaxAngle { get; set; } = 0.5f;
	public override float AngularVelucityInMinAngle { get; set; } = 4f;
	public override float HandBrakeForwardStiffness { get; set; } = 0.5f;
	public override float HandBrakeSidewaysStiffness { get; set; } = 0.5f;

	public override PacejkaCurve FrictionPreset { get; set; } = PacejkaCurve.Street;
}
