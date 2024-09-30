
namespace Sandbox.Utils;

[Hide]
public class Animation : Component
{
	public event Action<float> OnAnimationProgress;
	public event Action OnAnimationStart;
	public event Action OnAnimationEnd;

	private float progress = 0;
	private TimeSince T;
	public float Length { get; set; } = 1f;
	public int DelayRealtime { get; set; } = 0;

	public Curve Curve = new( new List<Curve.Frame>() { new( 0, 0 ), new( 1, 1 ) } );

	protected override void OnStart()
	{
		if ( DelayRealtime == 0 )
			Start();
		else
			DelayStart();
	}

	private async void DelayStart()
	{
		Enabled = false;
		await GameTask.DelayRealtime( DelayRealtime );
		Start();
	}

	private void Start()
	{
		T = 0;
		Enabled = true;
		OnAnimationStart?.Invoke();
	}

	protected override void OnDestroy()
	{
		OnAnimationEnd?.Invoke();
		base.OnDestroy();
	}

	protected override void OnUpdate()
	{

		OnAnimationProgress?.Invoke( Curve.Evaluate( progress ) );
		if ( progress == 1 )
			Destroy();

		progress = MathX.Approach( progress, 1, Time.Delta / Length );
	}
}
