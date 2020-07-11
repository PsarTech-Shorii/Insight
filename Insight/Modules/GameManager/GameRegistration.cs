using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class GameRegistration : InsightModule {
		private InsightClient _client;
		private Transport _transport;
		private NetworkManager _netManager;
		
		private IEnumerator _gameUpdater;

		//Pulled from command line arguments
		private int _ownerId;
		private string _uniqueId;
		private string _networkAddress;
		private ushort _networkPort;
		private string _sceneName;
		private string _gameName;
		private int _minPlayers;
		private int _maxPlayers;
		private int _currentPlayers;
		private bool _hasStarted;

		[SerializeField] private float updateDelayInSeconds = 1;

		public override void Initialize(InsightClient client, ModuleManager manager) {
			_client = client;
			_transport = Transport.activeTransport;
			_netManager = NetworkManager.singleton;

			Debug.Log("[GameRegistration] - Initialization");

			GatherCmdArgs();
			RegisterHandlers();

			_maxPlayers = _netManager.maxConnections;
			_netManager.StartServer();
		}

		private void RegisterHandlers() {
			_client.transport.OnClientConnected.AddListener(RegisterGame);
			_transport.OnServerConnected.AddListener(GameUpdate);
			_transport.OnServerDisconnected.AddListener(GameUpdate);
		}

		private void GatherCmdArgs() {
			var args = new InsightArgs();

			if (args.IsProvided(ArgNames.UniqueId)) {
				Debug.Log("[Args] - UniqueID: " + args.UniqueId);
				_uniqueId = args.UniqueId;
			}
			
			if (args.IsProvided(ArgNames.NetworkAddress)) {
				Debug.Log("[Args] - NetworkAddress: " + args.NetworkAddress);
				_networkAddress = args.NetworkAddress;
				_netManager.networkAddress = _networkAddress;
			}

			if (args.IsProvided(ArgNames.NetworkPort)) {
				Debug.Log("[Args] - NetworkPort: " + args.NetworkPort);
				_networkPort = (ushort) args.NetworkPort;

				if (_transport.GetType().GetField("port") != null) {
					_transport.GetType().GetField("port")
						.SetValue(_transport, (ushort) args.NetworkPort);
				}
			}

			if (args.IsProvided(ArgNames.GameName)) {
				Debug.Log("[Args] - GameName: " + args.GameName);
				_gameName = args.GameName;
			}
			
			if (args.IsProvided(ArgNames.MinPlayers)) {
				Debug.Log("[Args] - MinPlayers: " + args.MinPlayers);
				_minPlayers = args.MinPlayers;
			}
		}

		#region Sender

		private void RegisterGame() {
			Assert.IsTrue(_client.IsConnected);
			Debug.Log("[GameRegistration] - Registering game");
			
			_client.NetworkSend(new RegisterGameMsg {
				uniqueId = _uniqueId,
				networkAddress = _networkAddress,
				networkPort = _networkPort,
				gameName = _gameName,
				minPlayers = _minPlayers,
				maxPlayers = _maxPlayers,
				currentPlayers = _currentPlayers
			});
		}

		private void GameUpdate(int connectionId = -1) {
			if (_gameUpdater != null) StopCoroutine(_gameUpdater);
			_gameUpdater = GameUpdateCor();
			StartCoroutine(_gameUpdater);
		}

		private IEnumerator GameUpdateCor() {
			while (!_client.IsConnected) {
				yield return new WaitForSeconds(updateDelayInSeconds);
			}

			_currentPlayers = NetworkServer.connections.Count;
			
			var startedText = _hasStarted ? "started" : "not started";
			Debug.Log($"[GameRegistration] - Updating game : {_currentPlayers} players in the {startedText} game");
			_client.NetworkSend(new GameStatusMsg {
				uniqueId = _uniqueId,
				currentPlayers = _currentPlayers,
				hasStarted = _hasStarted
			});
		}

		public void StartGame() {
			_hasStarted = true;
			GameUpdate();
		}

		#endregion
	}
}