public sealed class Music : Component
{
	[Property] public SoundFile File;

	protected override void OnStart()
	{
		base.OnStart();

		SoundHandle handle = Sound.PlayFile( File );
		handle.Position = WorldPosition;
		handle.Occlusion = false;
		handle.Volume = 1f;
	}
}
