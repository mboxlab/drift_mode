using System.Text.Json.Serialization;
using Sandbox.Utils;
namespace Sandbox.Tuning;

public class TuningOptions : ITuningEvent
{
	[Property, JsonInclude] public virtual string Name { get; set; } = "";
	[JsonInclude] public Dictionary<string, TuningOptions> Options { get; set; }
	public void OnTuningChanged( TuningOptions option, float value )
	{
		if ( CarSaver.CanSave() )
			CarSaver.SaveActiveCar();
	}
}

public class TuningOptionsData
{
	[JsonInclude] public string Name { get; set; }
	[JsonInclude] public Dictionary<string, TuningOptions> Options { get; set; }
}

public interface ITuningEvent : ISceneEvent<ITuningEvent>
{
	public void OnTuningChanged( TuningOptions tuning, float value );
}
