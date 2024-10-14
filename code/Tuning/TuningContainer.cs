using Sandbox.Utils;

namespace Sandbox.Tuning;

public sealed class TuningContainer : ISaveData
{
	public class TuningEntry
	{
		public virtual CarTuning CarTuning { get; set; }

		public float Tint { get; set; } = 1f;
		public TuningEntry( CarTuning tuning )
		{
			CarTuning = tuning;
		}

		public virtual string GetSerialized()
		{
			return $"{CarTuning.ResourceId}:{Tint}";
		}
		public virtual void Apply( GameObject body ) { }
	}

	public List<TuningEntry> CarTuning = new();

	/// <summary>
	/// Add a tuning item if we don't already contain it, else remove it
	/// </summary>
	/// <param name="tuning"></param>
	public void Toggle( CarTuning tuning )
	{
		if ( Has( tuning ) )
			Remove( tuning );
		else
			Add( tuning );
	}
	/// <summary>
	/// Add a tuning item if we don't already contain it, else skip it
	/// </summary>
	/// <param name="tuning"></param>
	public bool TryAdd( CarTuning tuning )
	{
		if ( Has( tuning ) )
			return false;

		Add( tuning );
		return true;
	}
	/// <summary>
	/// Returns true if we have this tuning item
	/// </summary>
	/// <param name="tuning"></param>
	/// <returns></returns>
	public bool Has( CarTuning tuning )
	{
		return CarTuning.Any( ( TuningEntry x ) => x.CarTuning == tuning );
	}

	/// <summary>
	/// Remove tuning item
	/// </summary>
	/// <param name="tuning"></param>
	private void Remove( CarTuning tuning )
	{
		CarTuning.RemoveAll( ( TuningEntry x ) => x.CarTuning == tuning );
	}

	/// <summary>
	/// Add tuning item
	/// </summary>
	/// <param name="tuning"></param>
	/// <returns></returns>
	private TuningEntry Add( CarTuning tuning )
	{

		CarTuning.RemoveAll( ( TuningEntry x ) => !x.CarTuning.CanBeWornWith( tuning ) );
		var tuningEntry = tuning.GetEntry();
		CarTuning.Add( tuningEntry );
		return tuningEntry;
	}


	/// <summary>
	/// Clear the outfit from this model, make it named
	/// </summary>
	public static void Reset( SkinnedModelRenderer body )
	{
		//
		// Start with defaults
		//
		body.MaterialGroup = "default";
		body.MaterialOverride = null;

		//
		// Remove old models
		//
		foreach ( var children in body.GameObject.Children )
		{
			if ( children.Tags.Has( "tuning" ) )
			{
				children.Destroy();
			}
		}
	}

	/// <summary>
	/// Dress a skinned model renderer with an outfit
	/// </summary>
	public void Apply( SkinnedModelRenderer body )
	{
		// remove out outfit
		Reset( body );

		//
		// Create clothes models
		//
		foreach ( var entry in CarTuning )
		{
			var c = entry.CarTuning;
			var modelPath = c.GetModel( CarTuning.Select( x => x.CarTuning ).Except( new[] { c } ) );

			if ( string.IsNullOrEmpty( modelPath ) )
				continue;

			var model = Model.Load( modelPath );
			if ( model is null || model.IsError )
				continue;

			foreach ( var item in body.Model.Bones.AllBones )
				if ( Enum.TryParse( item.Name, out CarTuning.BodyGroups flag ) )
					if ( c.HideBody.HasFlag( flag ) )
					{
						GameObject go = new( false, $"Tuning - {c.ResourceName}" )
						{
							Parent = body.GetBoneObject( item )
						};

						go.Tags.Add( "tuning" );
						go.Tags.Add( c.Category.ToString() );

						var r = go.Components.Create<SkinnedModelRenderer>();
						r.Model = Model.Load( c.Model );
						entry.Apply( go );

						go.Enabled = true;
					}
		}

	}

	public string Save()
	{
		var itemString = "";

		foreach ( var item in CarTuning )
		{
			if ( item is null )
				itemString += "null,";
			else
				itemString += $"{item.GetSerialized()},";
		}

		if ( itemString.EndsWith( "," ) )
			itemString = itemString[..^1];

		return itemString;
	}

	public void Load( string data )
	{
		var allTunings = ResourceLibrary.GetAll<CarTuning>();
		var tunings = data.Split( ',' );
		var i = 0;

		foreach ( var tuning in tunings )
		{
			i++;

			if ( tuning == "null" )
				continue;

			var tuningData = tuning.Split( ':' );
			var id = int.Parse( tuningData[0] );
			var tuningResource = allTunings.FirstOrDefault( x => x.ResourceId == id );

			if ( tuningResource is null )
			{
				Log.Warning( $"Couldn't find tuning resource {tuningData[0]}" );
				continue;
			}
			CarTuning.Add( tuningResource.Parse( tuningData ) );
		}
	}
}
