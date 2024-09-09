using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DM.Vehicle;

namespace DM.Car;

public partial class Wheel : IWheel
{

	public float MotorTorque { get; set; }
	public float BrakeTorque { get; set; }

	private float _counterTorque { get; set; }
	public float CounterTorque => _counterTorque;

	public float RollingResistanceTorque { get; set; }
	public float SteerAngle { get; set; }

	private float _load { get; set; }
	public float Load => _load;

	[Property, Group( "Components" )] public FrictionPreset ForwardFriction { get; set; } = new();
	[Property, Group( "Components" )] public FrictionPreset SideFriction { get; set; } = new();
	[Property, Group( "Components" )] public Spring Spring { get; set; } = new();
	[Property, Group( "Components" )] public Damper Damper { get; set; } = new();
	[Property, Group( "Components" )] public Rigidbody Rigidbody { get; set; } = new();

	[Property]
	public float Radius
	{
		get => _radius;
		set
		{
			_inertia = 0.5f * (Mass * value * value);
			_radius = value;
		}
	}
	private float _radius { get; set; }

	[Property]
	public float Width { get; set; }

	[Property]
	public float Mass
	{
		get => _mass;
		set
		{
			_inertia = 0.5f * (value * Radius * Radius);
			_mass = value;
		}
	}
	private float _mass { get; set; }

	public float Inertia => _inertia;
	private float _inertia { get; set; }

}
