using Sandbox.Utils;

namespace Sandbox.Tuning;

public sealed class TuningList : Component, ICarSaverEvent
{
	[Property] public List<CarTuning> List { get; set; }
}
