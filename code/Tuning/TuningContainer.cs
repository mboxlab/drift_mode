
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Sandbox.Tuning;

public sealed class TuningContainer
{
	public class TuningEntry
	{
		public CarTuning CarTuning { get; set; }

		/// <summary>
		/// Used to select a tint
		/// </summary>
		public float? Tint { get; set; }

		public TuningEntry( CarTuning tuning )
		{
			CarTuning = tuning;
		}
	}

	/// <summary>
	/// Used for serialization
	/// </summary>
	public class Entry
	{
		[JsonPropertyName( "id" )]
		public int Id { get; set; }

		/// <summary>
		/// Tint variable used to evaluate the model tint color gradient
		/// </summary>
		[JsonPropertyName( "t" )]
		public float? Tint { get; set; }
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
		TuningEntry tuningEntry = new( tuning );
		CarTuning.Add( tuningEntry );
		return tuningEntry;
	}

	/// <summary>
	/// Find a tuning entry matching this tuning item
	/// </summary>
	/// <param name="tuning"></param>
	/// <returns></returns>
	public TuningEntry FindEntry( CarTuning tuning )
	{
		return CarTuning.Where( ( TuningEntry x ) => x.CarTuning == tuning ).FirstOrDefault();
	}

	private IEnumerable<Entry> GetSerialized()
	{
		foreach ( TuningEntry item in CarTuning )
		{
			yield return new Entry
			{
				Id = item.CarTuning.ResourceId,
				Tint = item.Tint
			};
		}
	}
	/// <summary>
	/// Serialize to Json
	/// </summary>
	/// <returns></returns>
	public string Serialize()
	{
		return JsonSerializer.Serialize( GetSerialized() );
	}
	/// <summary>
	/// Deserialize from Json
	/// </summary>
	/// <param name="json"></param>
	public void Deserialize( string json )
	{
		CarTuning.Clear();
		if ( string.IsNullOrWhiteSpace( json ) )
			return;

		try
		{
			JsonNode jsonNode = JsonNode.Parse( json );
			if ( jsonNode is JsonArray array )
			{
				ParseEntries( array );
			}
			else if ( jsonNode is JsonObject jsonObject )
			{
				ParseEntries( jsonObject["Items"] as JsonArray );
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( ex, $"Exception when deserailizing CarTuning ({ex.Message})" );
		}
	}

	private void ParseEntries( JsonArray array )
	{
		if ( array == null )
			return;

		Entry[] array2 = array.Deserialize<Entry[]>();
		foreach ( Entry entry in array2 )
		{
			CarTuning tuning = ResourceLibrary.Get<CarTuning>( entry.Id );
			if ( tuning != null )
				Add( tuning ).Tint = entry.Tint;
		}
	}

	/// <summary>
	/// Create the container from json definitions
	/// </summary>
	/// <param name="json"></param>
	/// <returns></returns>
	public static TuningContainer CreateFromJson( string json )
	{
		TuningContainer tuningContainer = new();
		tuningContainer.Deserialize( json );
		return tuningContainer;
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
			{
				if ( Enum.TryParse( item.Name, out CarTuning.BodyGroups flag ) )
				{
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

						if ( c.AllowTintSelect )
						{
							var tintValue = entry.Tint?.Clamp( 0, 1 ) ?? c.TintDefault;
							var tintColor = c.TintSelection.Evaluate( tintValue );
							r.Tint = tintColor;
						}
						go.Enabled = true;

					}
				}
			}

		}

	}
}
