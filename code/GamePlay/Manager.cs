

namespace Sandbox.GamePlay;

public interface IManager
{
	public bool Started { get; set; }
	int MaxLaps { get; set; }
	int Lap { get; set; }
	Checkpoint FirstCheckpoint { get; set; }
	int MaxPlayers { get; set; }
}
