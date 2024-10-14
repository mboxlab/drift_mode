using static Sandbox.Tuning.TuningContainer;

namespace Sandbox.Tuning;

[GameResource( "Car Tuning Definition", "tuning", "Describes the car tuning element and indirectly indicates which other elements it can be combined with.", Icon = "directions_car", IconBgColor = "gray", IconFgColor = "black" )]
public class CarTuning : GameResource
{
	public virtual TuningEntry GetEntry()
	{
		return new TuningEntry( this );
	}
	public enum CarTuningCategory
	{
		None,
		BrakeDisk,
		Wheel,
	}

	public enum CarCategory
	{
		All,
		Sport,
		OffRoad,
	}

	[Flags]
	public enum Slots
	{
		Body = 1,
		BrakeDisk = 2,
		Wheel = 4,
		// x = 8,
		// x = 0x10,
		// x = 0x20,
		// x = 0x40,
		// x = 0x80,
		// x = 0x100,
		// x = 0x200,
		// x = 0x400,
		// x = 0x800,
		// x = 0x1000,
		// x = 0x2000,
		// x = 0x4000,
		// x = 0x8000,
		// x = 0x10000,
		// x = 0x20000,
		// x = 0x40000,
		// x = 0x80000,
		// x = 0x100000,
		// x = 0x200000,
		// x = 0x400000,
		// x = 0x800000,
		// x = 0x1000000,
		// x = 0x2000000,
		// x = 0x4000000,
		// x = 0x8000000,
		// x = 0x10000000
	}
	[Flags]
	public enum BodyGroups
	{
		Root = 1,
		RL = 2,
		RR = 4,
		FL = 8,
		FR = 0x10,
		Spoiler = 0x20,
		BrakeDisk = 0x40,
	}

	/// <summary>
	/// Which slots this tuning takes on "outer" layer.
	/// </summary>
	[BitFlags, Category( "Body Slots" )] public virtual Slots SlotsOver { get; set; }

	/// <summary>
	/// Which slots this tuning takes on "inner" layer.
	/// </summary>
	[BitFlags, Category( "Body Slots" )] public virtual Slots SlotsUnder { get; set; }

	/// <summary>
	/// Name of the tuning to show in UI.
	/// </summary>
	[Category( "Display Information" )] public virtual string Title { get; set; }

	/// <summary>
	/// A subtitle for this tuning piece.
	/// </summary>
	[Category( "Display Information" )] public virtual string Subtitle { get; set; }


	/// <summary>
	/// What kind of tuning this is?
	/// </summary>
	[Category( "Display Information" )] public virtual CarTuningCategory Category { get; set; }
	/// <summary>
	/// To which category of car is it applied
	/// </summary>
	[Category( "Display Information" )] public virtual CarCategory CarAffected { get; set; } = CarCategory.All;

	/// <summary>
	/// A list of conditional models. (key) = tag(s), (value) = model
	/// </summary>
	[Category( "Tags & Condition" )] public virtual Dictionary<string, string> ConditionalModels { get; set; }

	[Category( "Tags & Condition" ), Editor( "tags" )] public virtual string Tags { get; set; }

	/// <summary>
	/// This should be a single word to describe the subcategory, and should match any
	/// other items you want to categorize in the same bunch.The work will be tokenized
	/// so it can become localized.
	/// </summary>
	[Category( "Display Information" )]
	public virtual string SubCategory { get; set; }

	/// <summary>
	/// The tuning to parent this too. It will be displayed as a variation of its parent
	/// </summary>
	[Category( "Display Information" )]
	public virtual CarTuning Parent { get; set; }


	[Category( "User Customization" )]
	public virtual bool AllowTintSelect { get; set; }

	[Category( "User Customization" )]
	[HideIf( nameof( AllowTintSelect ), false )]
	public virtual Gradient TintSelection { get; set; } = Color.White;

	[Category( "User Customization" )]
	[HideIf( nameof( AllowTintSelect ), false )]
	[Range( 0f, 1f, 0.01f, true, true )]
	public virtual float TintDefault { get; set; } = 0.5f;

	[BitFlags]
	[Category( "Tuning Setup" )]
	public virtual BodyGroups HideBody { get; set; }
	/// <summary>
	/// The model to bonemerge to the player when this tuning is equipped.
	/// </summary>
	[ResourceType( "vmdl" )]
	[Category( "Tuning Setup" )]
	public virtual string Model { get; set; }

	/// <summary>
	/// Tries to get the model for this current tuning. Takes into account any conditional model for other tuning.
	/// </summary>
	/// <param name="tuningList"></param>
	/// <returns></returns>
	public string GetModel( IEnumerable<CarTuning> tuningList )
	{

		foreach ( CarTuning tuning in tuningList )
		{
			if ( string.IsNullOrEmpty( tuning.Tags ) )
				continue;

			string[] array = tuning.Tags.Split( " " );
			foreach ( string key in array )
				if ( ConditionalModels != null && ConditionalModels.TryGetValue( key, out var value ) )
					return value;
		}
		return Model;
	}


	/// <summary>
	/// 	Return true if this item of tuning can be worn with the target item, at the same time.
	/// </summary>
	/// <param name="target"></param>
	/// <returns></returns>
	public bool CanBeWornWith( CarTuning target )
	{
		if ( target == this )
			return false;

		if ( (target.SlotsOver & SlotsOver) != 0 )
			return false;

		if ( (target.SlotsUnder & SlotsUnder) != 0 )
			return false;

		return true;
	}

	/// <summary>
	/// Dress this sceneobject with the passed tunings. Return the created tunings.
	/// </summary>
	/// <param name="car"></param>
	/// <param name="tuning"></param>
	/// <returns></returns>
	public static List<SceneModel> DressSceneObject( SceneModel car, IEnumerable<CarTuning> tuning )
	{
		List<SceneModel> list = new();
		SceneWorld world = car.World;
		foreach ( CarTuning item in tuning )
		{
			string model = item.GetModel( tuning.Except( new CarTuning[1] { item } ) );
			if ( !string.IsNullOrEmpty( model ) )
			{
				Model model2 = Sandbox.Model.Load( model );
				SceneModel sceneModel = new( world, model2, car.Transform );
				list.Add( sceneModel );

				car.AddChild( "tuning", sceneModel );

				if ( item.AllowTintSelect )
					sceneModel.ColorTint = item.TintSelection.Evaluate( item.TintDefault );

				sceneModel.Update( 0.1f );
			}
		}

		foreach ( var (name, value) in GetBodyGroups( tuning ) )
			car.SetBodyGroup( name, value );

		return list;
	}
	/// <summary>
	/// Return a list of bodygroups and what their value should be
	/// </summary>
	/// <param name="tuning"></param>
	/// <returns></returns>
	internal static IEnumerable<(string name, int value)> GetBodyGroups( IEnumerable<CarTuning> tuning )
	{
		BodyGroups mask = tuning.Select( ( CarTuning x ) => x.HideBody ).DefaultIfEmpty().Aggregate( ( BodyGroups a, BodyGroups b ) => a | b );
		yield return ("root", ((mask & BodyGroups.Root) != 0) ? 1 : 0);
		yield return ("rl", ((mask & BodyGroups.RL) != 0) ? 1 : 0);
		yield return ("rr", ((mask & BodyGroups.RR) != 0) ? 1 : 0);
		yield return ("fl", ((mask & BodyGroups.FL) != 0) ? 1 : 0);
		yield return ("fr", ((mask & BodyGroups.FR) != 0) ? 1 : 0);
	}

}
