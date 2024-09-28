using Sandbox.Car;
using Sandbox.Car.Config;
using System.Threading.Tasks;
namespace Sandbox.GamePlay.Street;



[Title( "Street Manager" )]
[Category( "Gameplay" )]
[Icon( "electrical_services" )]
public sealed class StreetManager : Component, Component.INetworkListener, IManager
{
	/// <summary>
	/// if 0 then == the number of spawnpoints
	/// </summary>
	[Property] public int MaxPlayers { get; set; }
	[Property, HostSync] public bool Started { get => started; set { started = value; if ( started ) StartRace(); } }
	[Property] public Checkpoint FirstCheckpoint { get; set; }
	[Property] public Checkpoint CurrentCheckpoint { get; set; }
	[Property] public GameObject Arrow { get; set; }
	[Property] public int MaxLaps { get; set; } = 1;
	[Property] public int Lap { get; set; }
	[HostSync] public bool CanStart { get; set; } = false;

	/// <summary>
	/// Create a server (if we're not joining one)
	/// </summary>
	[Property] public bool StartServer { get; set; } = true;

	[Property] public CarConfigEnum CarConfig { get; set; } = CarConfigEnum.Street;

	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property] public GameObject PlayerPrefab { get; set; }

	/// <summary>
	/// A list of points to choose from randomly to spawn the player in. If not set, we'll spawn at the
	/// location of the NetworkHelper object.
	/// </summary>
	[Property] public List<GameObject> SpawnPoints { get; set; }

	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( StartServer && !Networking.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			Networking.CreateLobby();
		}

		if ( MaxPlayers == 0 )
		{
			MaxPlayers = SpawnPoints.Count;
		}
		WaitingPlayers();
	}

	/// <summary>
	/// A client is fully connected to the server. This is called on the host.
	/// </summary>
	public void OnActive( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' has joined the game" );

		if ( !PlayerPrefab.IsValid() )
			return;

		//
		// Find a spawn location for this player
		//
		var startLocation = FindSpawnLocation().WithScale( 1 );

		// Spawn this object and make the client the owner

		var player = PlayerPrefab.Clone( startLocation, name: $"Player - {channel.DisplayName}" );

		player.Transform.Position = startLocation.PointToWorld( Vector3.Backward * player.GetBounds().Extents.y );

		player.NetworkSpawn( channel );
		// FIX ME

		var car = player.Components.Get<CarController>();

		if ( CarConfig == CarConfigEnum.Drift )
			car.CarConfig = new DriftCarConfig();
		else if ( CarConfig == CarConfigEnum.Street )
			car.CarConfig = new StreetCarConfig();

		car.ClientInit();

	}

	/// <summary>
	/// Find the most appropriate place to respawn
	/// </summary>
	Transform FindSpawnLocation()
	{
		if ( SpawnPoints is not null && SpawnPoints.Count > 0 )
			return SpawnPoints[Connection.All.Count - 1].Transform.World;

		// Failing that, spawn where we are
		return Transform.World;
	}

	public int SecondsToStart = 5;
	private bool started = false;

	async void WaitingPlayers()
	{
		while ( !Started )
		{
			await Task.DelayRealtimeSeconds( 5f );
			if ( Connection.All.Count == MaxPlayers )
			{
				CanStart = true;
				while ( !Started )
				{
					await Task.DelayRealtimeSeconds( 1f );
					SecondsToStart--;
					if ( SecondsToStart == 0 )
					{
						await Task.DelayRealtimeSeconds( 1f );
						StartRace();
					}
				}
			}
		}
	}
	public void StartRace()
	{
		CurrentCheckpoint = FirstCheckpoint;
		CurrentCheckpoint.Trigger.OnTriggerEnter += OnTriggerEnter;
		started = true;
	}
	protected override void OnUpdate()
	{
		Arrow.Transform.Position = CarController.Local.Transform.Position + Vector3.Up * 125f;
		Arrow.Transform.Rotation = (CurrentCheckpoint.Transform.Position.WithZ( 0 ) - CarController.Local.Transform.Position.WithZ(0) + Vector3.Down * 30).EulerAngles;
	}
	private void Win()
	{
		CurrentCheckpoint.Trigger.OnTriggerEnter -= OnTriggerEnter;
		started = false;
	}

	private void OnTriggerEnter( Collider collider )
	{
		if ( CurrentCheckpoint == FirstCheckpoint )
		{
			if ( Lap == MaxLaps )
			{
				Win();
				return;
			}
			Lap++;
		}
		CurrentCheckpoint.Trigger.OnTriggerEnter -= OnTriggerEnter;
		CurrentCheckpoint = CurrentCheckpoint.Next;
		CurrentCheckpoint.Trigger.OnTriggerEnter += OnTriggerEnter;

	}

}
