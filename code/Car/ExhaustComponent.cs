
using System.Collections.Generic;
using Sandbox.Powertrain;

namespace Sandbox.Car;

public sealed class ExhaustComponent : Component
{
	[Property, Group( "Smoke" )] public List<ParticleEffect> Emitters { get; set; }
	[Property, Group( "Smoke" )] public EngineComponent Engine { get; set; }
	[Property, Group( "Smoke" )] public float MinRate { get; set; } = 5;
	[Property, Group( "Smoke" )] public float MaxRate { get; set; } = 50;

	/// <summary>
	/// How much soot is emitted when throttle is pressed.
	/// </summary>
	[Property, Range( 0, 1 ), Group( "Smoke" )] public float SootIntensity { get; set; } = 0.4f;

	/// <summary>
	/// Particle start speed is multiplied by this value based on engine RPM.
	/// </summary>
	[Property, Range( 1, 5 ), Group( "Smoke" )] public float MaxSpeedMultiplier { get; set; } = 1.4f;

	/// <summary>
	/// Particle start size is multiplied by this value based on engine RPM.
	/// </summary>
	[Property, Range( 1, 5 ), Group( "Smoke" )] public float MaxSizeMultiplier { get; set; } = 1.2f;

	/// <summary>
	/// Normal particle start color. Used when there is no throttle - engine is under no load.
	/// </summary>
	[Property, Group( "Smoke" )] public Color NormalColor { get; set; } = new( 0.6f, 0.6f, 0.6f, 0.3f );

	/// <summary>
	/// Soot particle start color. Used under heavy throttle - engine is under load.
	/// </summary>
	[Property, Group( "Smoke" )] public Color SootColor { get; set; } = new( 0.1f, 0.1f, 0.8f );
	[Property, Group( "Smoke" )] public Texture SmokeTexture { get; set; }


	[Property, Range( 0, 1 ), Group( "Flash" )] public float FlashChance = 0.2f;
	[Property, Group( "Flash" )] public List<ModelRenderer> FlashRenderers { get; set; }
	[Property, Group( "Flash" )] public List<Material> FlashMaterials { get; set; }
	[Property, Group( "Flash" )] public List<SoundFile> PopSounds { get; set; }

	private float _initStartSpeedMin;
	private float _initStartSpeedMax;
	private float _initStartSizeMin;
	private float _initStartSizeMax;
	private float _sootAmount;
	private float _vehicleSpeed;
	private ParticleFloat _minMaxCurve;

	protected override void OnStart()
	{
		base.OnStart();
		Engine.OnRevLimiter += OnRevLimiter;
		_initStartSpeedMin = Emitters[0].StartVelocity.ConstantA;
		_initStartSpeedMax = Emitters[0].StartVelocity.ConstantB;

		_initStartSizeMin = Emitters[0].Scale.ConstantA;
		_initStartSizeMax = Emitters[0].Scale.ConstantB;

		foreach ( var item in FlashRenderers )
			item.Enabled = false;

	}
	protected override void OnUpdate()
	{
		base.OnUpdate();
		//foreach ( var item in Emitters )
		//	item.Rate = MinRate.LerpTo( MaxRate, Engine.RPMPercent / (Engine.CarController.CurrentSpeed + 1) );

		float engineLoad = Engine.Load;
		float rpmPercent = Engine.RPMPercent;
		_sootAmount = engineLoad * SootIntensity;
		foreach ( var item in Emitters )
		{
			item.Enabled = Engine.Enabled;
			// Color
			item.Tint = Color.Lerp( item.Tint, Color.Lerp( NormalColor, SootColor, _sootAmount ), Time.Delta * 7f );
			item.Tint = item.Tint.WithAlphaMultiplied( 10 / (Engine.CarController.CurrentSpeed + 10) );
			//// Speed
			float speedMultiplier = MaxSpeedMultiplier - 1f;
			_minMaxCurve = item.StartVelocity;
			_minMaxCurve.ConstantA = _initStartSpeedMin + rpmPercent * speedMultiplier;
			_minMaxCurve.ConstantB = _initStartSpeedMax + rpmPercent * speedMultiplier;
			item.StartVelocity = _minMaxCurve;


			//// Size
			float sizeMultiplier = MaxSizeMultiplier - 1f;
			_minMaxCurve = item.Scale;
			_minMaxCurve.ConstantA = _initStartSizeMin + rpmPercent * sizeMultiplier;
			_minMaxCurve.ConstantB = _initStartSizeMax + rpmPercent * sizeMultiplier;
			item.Scale = _minMaxCurve;

		}
	}
	private async void OnRevLimiter()
	{
		if ( Game.Random.Float( 0f, 1f ) < FlashChance )
		{
			foreach ( var item in FlashRenderers )
			{
				item.Enabled = true;
				item.MaterialOverride = FlashMaterials[Game.Random.Int( 0, FlashMaterials.Count - 1 )];
				var snd = Sound.PlayFile( PopSounds[Game.Random.Int( 0, PopSounds.Count - 1 )] );
				snd.Occlusion = false;
				snd.Volume = 10f;
			}
			await GameTask.Delay( 100 );
			foreach ( var item in FlashRenderers )
			{
				item.Enabled = false;
			}
		}
	}
}
