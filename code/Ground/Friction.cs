public class Friction
{

	/// <summary>
	///     Current force in friction direction.
	/// </summary>
	[ReadOnly] public float Force { get; set; }

	/// <summary>
	/// Speed at the point of contact with the surface.
	/// </summary>
	[ReadOnly] public float Speed { get; set; }

	/// <summary>
	///     Multiplies the Y value (grip) of the friction graph.
	/// </summary>
	[Property] public float Grip { get; set; } = 1f;

	/// <summary>
	///     Current slip in friction direction.
	/// </summary>
	[ReadOnly] public float Slip { get; set; }
}
