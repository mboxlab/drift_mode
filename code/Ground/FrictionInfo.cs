public struct WheelFrictionInfo
{
	public float ExtremumSlip { get; set; } = 1.0f;
	public float ExtremumValue { get; set; } = 20000.0f;
	public float AsymptoteSlip { get; set; } = 2.0f;
	public float AsymptoteValue { get; set; } = 10000.0f;
	public float Stiffness { get; set; } = 1.0f;

	public WheelFrictionInfo()
	{
	}

	public float Evaluate( float slip )
	{
		var value = 0.0f;

		if ( slip <= ExtremumSlip )
		{
			value = (slip / ExtremumSlip) * ExtremumValue;
		}
		else
		{
			value = ExtremumValue - ((slip - ExtremumSlip) / (AsymptoteSlip - ExtremumSlip)) * (ExtremumValue - AsymptoteValue);
		}

		return (value * Stiffness).Clamp( 0, float.MaxValue );
	}
}
