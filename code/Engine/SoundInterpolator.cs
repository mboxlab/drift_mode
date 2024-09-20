using System;
namespace DM.Engine;

[Category( "Audio" )]
public sealed class SoundInterpolator : Component
{
	[Property] public GameObject Parent { get; set; }
	[Property] public Dictionary<int, SoundFile> Sounds { get; set; }
	private Dictionary<int, SoundHandle> SoundHandles { get; set; }
	private List<int> SoundTimes { get; set; }
	[Property] public float MaxValue { get; set; }
	[Property] public float Value { get; set; }
	[Property, Range( 0, 1 )] public float Volume { get; set; }

	private float smoothValue = 0;
	private float smoothVolume = 0;
	private float realMaxValue = 0;
	protected override void OnStart()
	{
		LoadSoundsAsync();
	}
	protected override void OnUpdate()
	{

		smoothValue = smoothValue * (1 - 0.2f) + Value * (realMaxValue / MaxValue) * 0.2f;
		smoothVolume = smoothVolume * (1 - 0.1f) + Volume * 0.1f;

		for ( int n = 0; n < SoundTimes.Count; n++ )
		{
			var time = SoundTimes[n]; // this
			float min = (n == 0) ? -100000f : SoundTimes[n - 1]; // prev
			float max = n == (SoundTimes.Count - 1) ? 100000f : SoundTimes[n + 1]; // next

			float c = Fade( smoothValue, min - 10f, time, max + 10 );
			float vol = c * Map( smoothVolume, 0f, 1f, 0.5f, 1f );

			SoundHandle soundObject = SoundHandles[time];
			soundObject.Volume = vol;
			soundObject.Pitch = smoothValue / time;

			soundObject.Position = Parent.Transform.Position;
		}

	}

	private async void LoadSoundsAsync()
	{
		SoundTimes = new();
		SoundHandles = new();
		foreach ( KeyValuePair<int, SoundFile> item in Sounds )
		{
			await item.Value.LoadAsync();
			SoundHandle snd = Sound.PlayFile( item.Value );
			snd.Volume = 0;
			snd.Occlusion = false;
			SoundTimes.Add( item.Key );
			SoundHandles.Add( item.Key, snd );
			realMaxValue = MathF.Max( realMaxValue, item.Key );
		}
	}

	private static float Map( float x, float a, float b, float c, float d ) => (x - a) / (b - a) * (d - c) + c;


	private static float Fade( float n, float min, float mid, float max )
	{
		if ( n < min || n > max )
			return 0;

		if ( n > mid )
			min = mid - (max - mid);

		return MathF.Cos( (1 - ((n - min) / (mid - min))) * (MathF.PI / 2) );
	}
}
