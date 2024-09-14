
namespace DM.Engine.Gearbox;
[Category( "Vehicles" )]
public class ManualGearbox : BaseGearbox
{
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( !CanShift() )
			return;

		if ( Input.Pressed( "GearUp" ) )
			Shift( 1 );
		else if ( Input.Pressed( "GearDown" ) )
			Shift( -1 );

	}
}
