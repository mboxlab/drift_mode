using System.Text.Json;
using System.Text.Json.Serialization;
using Sandbox.Car;
using Sandbox.Utils;
using static Sandbox.PhysicsGroupDescription.BodyPart;

namespace Sandbox.Tuning;

public class CarTuning
{
	[Hide, JsonIgnore] private float _value;

	[JsonInclude] public string Name { get; set; }
	[JsonInclude] public float MinValue { get; set; }

	[JsonInclude]
	public float Value
	{
		get => _value;
		set
		{
			if ( _value != value )
				_value = value;
		}
	}

	[JsonIgnore, Hide]
	public float BindValue
	{
		get => _value;
		set
		{
			if ( _value != value )
			{
				_value = value;

				CarController.Local.UpdateTuning();
				if ( CarSaver.CanSave() )
					CarSaver.SaveActiveCar();
			}

		}
	}

	[JsonInclude] public float MaxValue { get; set; }

}

public class TuningOption : Component, ITuningEvent, IJsonConvert
{
	[Property] public Action OnApply;
	[JsonInclude] public virtual string Name { get; set; } = "";
	[JsonInclude] public virtual List<CarTuning> Options { get; set; }
	[Property, ReadOnly] public virtual int Count { get => Options.Count; }

	public virtual void Set( int id, float value ) => Options[id].Value = value;
	public virtual CarTuning Get( int id ) => Options[id];

	public TuningOption GetOpiton()
	{
		return this;
	}
	private record struct Reference( [property: JsonPropertyName( "Name" )] string Name, [property: JsonPropertyName( "Options" )] List<CarTuning> Options );

	public static new object JsonRead( ref Utf8JsonReader reader, Type targetType )
	{
		if ( reader.TokenType == JsonTokenType.StartObject )
		{
			Reference componentReferenceModel = JsonSerializer.Deserialize<Reference>( ref reader );
			return new TuningOption() { Name = componentReferenceModel.Name, Options = componentReferenceModel.Options };
		}

		return null;
	}
	public static new void JsonWrite( object value, Utf8JsonWriter writer )
	{
		if ( value is not TuningOption component )
			throw new NotImplementedException();

		if ( !component.IsValid )
		{
			writer.WriteNullValue();
			return;
		}

		Reference value2 = new( component.Name, component.Options );
		JsonSerializer.Serialize( writer, value2 );
	}

	public void Apply()
	{
		OnApply?.Invoke();
	}
}


public interface ITuningEvent : ISceneEvent<ITuningEvent>
{
	public TuningOption GetOpiton();
}
