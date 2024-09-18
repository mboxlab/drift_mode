namespace DM.Car;
//
// Summary:
//     A piece of car model customization.
public class CarPart : GameResource
{
	public enum PartCategory
	{
		None,
		Bumper,
		//Hood,
		//SideSkirt,
		//Spoiler,
		//Wheel,
		//Tyre,
		//Engine,
		//RollCage,
		//Seat,
		//SteeringWheel,
		//GearStick,
		//Handbrake,
		//Gauge,
		//HeadUnits,
		//Speaker,
	}


	//
	// Summary:
	//     The clothing to parent this too. It will be displayed as a variation of its parent
	[Category( "Display Information" )]
	[Description( "If this part is a variation of another set it here" )]
	public CarPart Parent { get; set; }


	//
	// Summary:
	//     The model to bonemerge to the player when this clothing is equipped.
	[ResourceType( "vmdl" )]
	[Category( "Part Setup" )]
	public string Model { get; set; }
}
