using System;

namespace DM.Vehicle;

public class FrictionPreset
{
	//public const int LUT_RESOLUTION = 1000;

	/// <summary>
	///     B, C, D and E parameters of short version of Pacejka's magic formula.
	/// </summary>
	//[Property] public Vector4 BCDE;

	/// <summary>
	/// Slip at which the friction preset has highest friction.
	/// </summary>
	public float PeakSlip { get; set; }

	[Property] public Curve Curve { get; set; }


	///// <summary>
	///// Gets the slip at which the friction is the highest for this friction curve.
	///// </summary>
	///// <returns></returns>
	//public float GetPeakSlip()
	//{
	//	float peakSlip = -1;
	//	float yMax = 0;

	//	for ( float i = 0; i < 1f; i += 0.01f )
	//	{
	//		float y = Curve.Evaluate( i );
	//		if ( y > yMax )
	//		{
	//			yMax = y;
	//			peakSlip = i;
	//		}
	//	}

	//	return peakSlip;
	//}


	///// <summary>
	/////     Generate Curve from B,C,D and E parameters of Pacejka's simplified magic formula
	///// </summary>
	//public void UpdateFrictionCurve()
	//{
	//	Curve = new Curve();
	//	int n = 20;
	//	float t = 0;

	//	for ( int i = 0; i < n; i++ )
	//	{
	//		float v = GetFrictionValue( t, BCDE );
	//		Curve.AddPoint( t, v );

	//		if ( i <= 10 )
	//		{
	//			t += 0.02f;
	//		}
	//		else
	//		{
	//			t += 0.1f;
	//		}
	//	}

	//	PeakSlip = GetPeakSlip();
	//}


	//private static float GetFrictionValue( float slip, Vector4 p )
	//{
	//	float B = p.x;
	//	float C = p.y;
	//	float D = p.z;
	//	float E = p.w;
	//	float t = MathF.Abs( slip );
	//	return D * MathF.Sin( C * MathF.Atan( B * t - E * (B * t - MathF.Atan( B * t )) ) );
	//}
}
