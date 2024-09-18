
using System;

namespace DM.Engine.Gearbox;

public abstract class BaseGearbox : Component
{
	private int gear;

	[Property, ReadOnly]
	public int Gear
	{
		get => gear;
		set
		{
			gear = value;
			ShiftTime = 0;
			CalcRatio();
		}
	}
	[Property] public EngineICE Engine { get; set; }
	[Property] public Clutch Clutch { get; set; }
	[Property] public List<Differential> Axles { get; set; }
	[Property] public float[] Ratios { get; set; }
	[Property] public float ReverseRatio { get; set; } = 3.082f;
	[Property] public float ShiftDuration { get; set; } = 0.14f;
	[Property, ReadOnly] public float RPM { get; set; }
	[Property, ReadOnly] public float Ratio { get; set; }
	[Property, ReadOnly] public float Torque { get; set; }
	private TimeSince ShiftTime { get; set; }
	public virtual void SetGear( int gear )
	{
		if ( gear >= -1 && gear <= Ratios.Length )
			Gear = gear;
	}
	public virtual void Shift( int dir )
	{
		SetGear( Gear + Math.Sign( dir ) );
	}
	public virtual bool CanShift()
	{
		return ShiftTime > ShiftDuration;
	}
	public virtual void CalcRatio()
	{
		if ( Gear <= -1 )
			Ratio = -ReverseRatio;
		else if ( Gear == 0 )
			Ratio = 0;
		else
			Ratio = Ratios[Gear - 1];
	}
	internal void Think()
	{
		int isShifting = CanShift() ? 1 : 0;

		Torque = Clutch.Torque * Ratio * isShifting;
		Axles.ForEach( x => x.Think() );

		RPM = Axles.MaxBy( x => x.AverageRPM ).AverageRPM * Ratio;
	}
}
