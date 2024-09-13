
using System;

namespace DM.Car;

/// <summary>
///     Suspension spring.
/// </summary>
[Serializable]
public class Spring
{
	public enum ExtensionState
	{
		Normal,
		OverExtended,
		BottomedOut
	}
	public Spring()
	{
		MaxForce = 16000.0f;
		MaxLength = 12f;
		ForceCurve = new Curve();
	}
	/// <summary>
	/// The state the spring is in.
	/// </summary>s
	public ExtensionState State = ExtensionState.Normal;

	/// <summary>
	///     How much is spring currently compressed. 0 means fully relaxed and 1 fully compressed.
	/// </summary>
	public float Compression;

	/// <summary>
	///     Current force the spring is exerting in [N].
	/// </summary>
	public float Force;

	/// <summary>
	///     Force curve where X axis represents spring travel [0,1] and Y axis represents force coefficient [0, 1].
	///     Force coefficient is multiplied by maxForce to get the final spring force.
	/// </summary>
	public Curve ForceCurve { get; set; }

	/// <summary>
	///     Maximum force spring can exert in Nm.
	/// </summary>
	public float MaxForce { get; set; }

	/// <summary>
	///     Length of fully relaxed spring in Inches.
	/// </summary>
	public float MaxLength { get; set; }

	/// <summary>
	///     Current length of the spring.
	/// </summary>
	public float Length;

	/// <summary>
	///     Length of the spring during the previous physics update.
	/// </summary>
	public float PrevLength;

	/// <summary>
	///     Rate of change of the length of the spring in [m/s].
	/// </summary>
	public float CompressionVelocity;

	/// <summary>
	/// Velocity of the spring in the previous frame.
	/// </summary>
	public float PrevVelocity;
}

