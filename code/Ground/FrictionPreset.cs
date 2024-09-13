using System;
using static Sandbox.Citizen.CitizenAnimationHelper;
using System.Diagnostics;
using static Sandbox.Curve;

namespace DM.Ground;

public class FrictionPreset
{
	private float stiffnes = 12.5f;
	private float shapeFactor = 2.05f;
	private float peakValue = 0.925f;
	private float curvatureFactor = 0.97f;

	public FrictionPreset( float stiffnes, float shapeFactor, float peakValue, float curvatureFactor )
	{
		Stiffnes = stiffnes;
		ShapeFactor = shapeFactor;
		PeakValue = peakValue;
		CurvatureFactor = curvatureFactor;
		UpdateFrictionCurve();
	}

	public FrictionPreset()
	{
		UpdateFrictionCurve();
	}


	// X // B
	[Property, Range( 0, 30 )] public float Stiffnes { get => stiffnes; set { stiffnes = value; UpdateFrictionCurve(); } }

	// Y // C
	[Property, Range( 0, 5 )] public float ShapeFactor { get => shapeFactor; set { shapeFactor = value; UpdateFrictionCurve(); } }

	// Z // D
	[Property, Range( 0, 2 )] public float PeakValue { get => peakValue; set { peakValue = value; UpdateFrictionCurve(); } }

	// W // E
	[Property, Range( 0, 2 )] public float CurvatureFactor { get => curvatureFactor; set { curvatureFactor = value; UpdateFrictionCurve(); } }

	/// <summary>
	/// Slip at which the friction preset has highest friction.
	/// </summary>
	public float PeakSlip { get; set; }

	[Property] public Curve Curve { get; set; }

	/// <summary>
	/// Gets the slip at which the friction is the highest for this friction curve.
	/// </summary>
	public float GetPeakSlip()
	{
		float peakSlip = -1;
		float yMax = 0;

		for ( float i = 0; i < 1f; i += 0.01f )
		{
			float y = Curve.Evaluate( i );
			if ( y > yMax )
			{
				yMax = y;
				peakSlip = i;
			}
		}

		return peakSlip;
	}


	/// <summary>
	///     Generate Curve from B,C,D and E parameters of Pacejka's simplified magic formula
	/// </summary>
	public void UpdateFrictionCurve()
	{
		Frame[] frames = new Frame[20];
		float t = 0;

		for ( int i = 0; i < frames.Length; i++ )
		{
			float v = GetFrictionValue( t );
			frames[i] = new Frame( t, v );

			if ( i <= 10 )
			{
				t += 0.02f;
			}
			else
			{
				t += 0.1f;
			}
		}
		Curve = new Curve( frames );

		PeakSlip = GetPeakSlip();
	}


	private float GetFrictionValue( float slip )
	{
		float B = Stiffnes;
		float C = ShapeFactor;
		float D = PeakValue;
		float E = CurvatureFactor;
		float t = MathF.Abs( slip );
		return D * MathF.Sin( C * MathF.Atan( B * t - E * (B * t - MathF.Atan( B * t )) ) );
	}

	public static readonly FrictionPreset Asphalt = new( 9f, 2.15f, 0.933f, 0.871f );
	public static readonly FrictionPreset AsphaltWet = new( 9f, 2.35f, 0.82f, 0.907f );
	public static readonly FrictionPreset Generic = new( 8f, 1.9f, 0.8f, 0.99f );
	public static readonly FrictionPreset Grass = new( 7.38f, 1.1f, 0.538f, 1f );
	public static readonly FrictionPreset Gravel = new( 5.39f, 1.03f, 0.634f, 1f );
	public static readonly FrictionPreset Ice = new( 1.2f, 2f, 0.16f, 1f );
	public static readonly FrictionPreset Rock = new( 7.24f, 2.11f, 0.59f, 1f );
	public static readonly FrictionPreset Sand = new( 5.13f, 1.2f, 0.443f, 0.5f );
	public static readonly FrictionPreset Snow = new( 8.5f, 1.1f, 0.4f, 0.9f );
	public static readonly FrictionPreset Tracks = new( 0.1f, 2f, 2f, 1f );
	public static readonly FrictionPreset Arcade = new( 7.09f, 0.87f, 2f, 0.5f );

	public static Dictionary<PresetsEnum, FrictionPreset> Presets = new()
	{
		{PresetsEnum.Asphalt      ,    Asphalt},
		{PresetsEnum.AsphaltWet   , AsphaltWet},
		{PresetsEnum.Generic      ,    Generic},
		{PresetsEnum.Grass        ,      Grass},
		{PresetsEnum.Gravel       ,     Gravel},
		{PresetsEnum.Ice          ,        Ice},
		{PresetsEnum.Rock         ,       Rock},
		{PresetsEnum.Sand         ,       Sand},
		{PresetsEnum.Snow         ,       Snow},
		{PresetsEnum.Tracks       ,     Tracks},
		{PresetsEnum.Arcade       ,     Arcade}
	};

	public enum PresetsEnum
	{
		Asphalt,
		AsphaltWet,
		Generic,
		Grass,
		Gravel,
		Ice,
		Rock,
		Sand,
		Snow,
		Tracks,
		Arcade
	}

	public void To( FrictionPreset preset )
	{
		stiffnes = preset.Stiffnes;
		shapeFactor = preset.ShapeFactor;
		peakValue = preset.PeakValue;
		curvatureFactor = preset.CurvatureFactor;
		UpdateFrictionCurve();
	}
}
