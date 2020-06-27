using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Insight {
	public class GameRegistration : InsightModule {
		private InsightClient _client;
		
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
		private bool _hasStarted = false;

		private IEnumerator _gameUpdater;
		private Transport _networkManagerTransport;
		
		[SerializeField] private float updateDelayInSeconds = 1;

		public override void Initialize(InsightClient client, ModuleManager manager) {
			_client = client;
			_networkManagerTransport = Transport.activeTransport;
			
			Debug.Log("[GameRegistration] - Initialization");

			RegisterHandlers();
			GatherCmdArgs();
			
			_maxPlayers = NetworkManager.singleton.maxConnections;
			NetworkManager.singleton.StartServer();
		}

		private void RegisterHandlers() {
			_client.transport.OnClientConnected.AddListener(RegisterGame);
			_networkManagerTransport.OnServerConnected.AddListener(HandleConnection);
			_networkManagerTransport.OnServerDisconnected.AddListener(HandleConnection);
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
				NetworkManager.singleton.networkAddress = _networkAddress;
			}

			if (args.IsProvided(ArgNames.NetworkPort)) {
				Debug.Log("[Args] - NetworkPort: " + args.NetworkPort);
				_networkPort = (ushort) args.NetworkPort;

				if (_networkManagerTransport.GetType().GetField("port") != null) {
					_networkManagerTransport.GetType().GetField("port")
						.SetValue(_networkManagerTransport, (ushort) args.NetworkPort);
				}
			}

			if (args.IsProvided(ArgNames.SceneName)) {
				Debug.Log("[Args] - SceneName: " + args.SceneName);
				_sceneName = args.SceneName;
				SceneManager.LoadScene(args.SceneName);
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
				sceneName = _sceneName,
				gameName = _gameName,
				minPlayers = _minPlayers,
				maxPlayers = _maxPlayers,
				currentPlayers = _currentPlayers
			});
		}

		private void HandleConnection(int connectionId = -1) {
			if (_gameUpdater != null) StopCoroutine(_gameUpdater);
			_gameUpdater = GameUpdateCor();
			StartCoroutine(_gameUpdater);
		}

		private IEnumerator GameUpdateCor() {
			while (!_client.IsConnected) {
				yield return new WaitForSeconds(updateDelayInSeconds);
			}
			
			yield return new WaitForEndOfFrame();

			Debug.Log($"[GameRegistration] - Updating game : {NetworkManager.singleton.numPlayers} players");
			_currentPlayers = NetworkManager.singleton.numPlayers;
			_client.NetworkSend(new GameStatusMsg {
				uniqueId = _uniqueId,
				currentPlayers = _currentPlayers,
				hasStarted = _hasStarted
			});
		}

		#endregion
	}
}