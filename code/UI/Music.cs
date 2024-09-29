using Sandbox;

public sealed class Music : Component
{
	[Property] public SoundFile File;

	protected override void OnStart()
	{
		base.OnStart();

		SoundHandle handle = Sound.PlayFile( File );
		handle.Position = Transform.Position;
		handle.Occlusion = false;
		handle.Volume = 1f;
	}
}
