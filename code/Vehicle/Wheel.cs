using System;
using System.Numerics;

namespace DM.Vehicle;
/// <summary>
///     Contains everything wheel related.
/// </summary>
[Serializable]
public class Wheel
{

	/// <summary>
	///     GameObject representing the visual aspect of the wheel / wheel mesh.
	///     Should not have any physics colliders attached to it.
	/// </summary>
	[Property] public Renderer Visual;

	/// <summary>
	///     Current angular velocity of the wheel in rad/s.
	/// </summary>
	public float AngularVelocity;

	/// <summary>
	///     Current wheel RPM.
	/// </summary>
	[Property]
	public float RPM
	{
		get { return AngularVelocity * 9.5493f; }
	}

	/// <summary>
	///     Forward vector of the wheel in world coordinates.
	/// </summary>
	[NonSerialized]
	public Vector3 Forward;

	/// <summary>
	///     Vector in world coordinates pointing to the right of the wheel.
	/// </summary>
	[NonSerialized]
	public Vector3 Right;

	/// <summary>
	///     Wheel's up vector in world coordinates.
	/// </summary>
	[NonSerialized]
	public Vector3 Up;

	/// <summary>
	/// Total inertia of the wheel and any attached components.
	/// </summary>
	[Property]
	public float Inertia;

	/// <summary>
	///     Mass of the wheel. Inertia is calculated from this.
	/// </summary>
	[Property]
	public float Mass
	{
		get => _mass;
		set
		{
			Inertia = 0.5f * (value * Radius * Radius);
			_mass = value;
		}
	}
	private float _mass;

	/// <summary>
	///     Total radius of the tire in [m].
	/// </summary>
	[Property]
	public float Radius
	{
		get => _radius;
		set
		{
			Inertia = 0.5f * (value * Radius * Radius);
			_radius = value;
		}
	}
	private float _radius;

	/// <summary>
	///     Width of the tyre.
	/// </summary>
	[Property]
	public float Width
	{
		get => _width;
		set
		{
			Inertia = 0.5f * (value * Radius * Radius);
			_width = value;
		}
	}
	private float _width;

	/// <summary>
	///     Current rotation angle of the wheel visual in regards to it's X axis vector.
	/// </summary>
	[NonSerialized]
	internal float AxleAngle = new();

	/// <summary>
	///     Position of the wheel in the previous physics update in world coordinates.
	/// </summary>
	[NonSerialized]
	internal Vector3 prevWorldPosition = new();

	/// <summary>
	///     Position of the wheel relative to the WheelController transform.
	/// </summary>
	[NonSerialized]
	internal Vector3 localPosition = new();

	/// <summary>
	///     Angular velocity during the previus FixedUpdate().
	/// </summary>
	[NonSerialized]
	internal float prevAngularVelocity = new();

	/// <summary>
	///     Rotation of the wheel in world coordinates.
	/// </summary>
	[NonSerialized]
	internal Quaternion worldRotation = new();

	/// <summary>
	/// Local rotation of the wheel.
	/// </summary>
	[NonSerialized] internal Quaternion localRotation = new();

	/// <summary>
	/// Width of the wheel during the previous frame.
	/// </summary>
	[NonSerialized] internal float prevWidth = new();

	/// <summary>
	/// Radius of the wheel during the previous frame.
	/// </summary>
	[NonSerialized] internal float prevRadius = new();

	public void UpdateProperties()
	{
		Inertia = 0.5f * (Mass * Radius * Radius);
	}

}
