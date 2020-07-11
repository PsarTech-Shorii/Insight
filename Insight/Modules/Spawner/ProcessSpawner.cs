using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace Insight {
	public class ProcessSpawner : InsightModule {
		private InsightServer _server;
		private InsightClient _client;
		private RunningProcessContainer[] _spawnerProcesses;
		private bool _registrationComplete;
		private string _uniqueId;

		private const string Space = " ";

		[Header("Network")] 
		[Tooltip("NetworkAddress that spawned processes will use")]
		public string spawnerNetworkAddress = "localhost";

		[Tooltip("Port that will be used by the NetworkManager in the spawned game")]
		public int startingNetworkPort = 7777; //Default port of the NetworkManager.

		[Header("Paths")] 
		public string editorPath;
		public string processPath;
		public string processName;

		[Header("Threads")]
		public int maximumProcesses = 5;

		public override void Initialize(InsightServer server, ModuleManager manager) {
			_server = server;
			
			Debug.Log("[Server - ProcessSpawner] - Initialization");
			
			RegisterHandlers();
		}

		public override void Initialize(InsightClient client, ModuleManager manager) {
			_client = client;
			
			Debug.Log("[Client - ProcessSpawner] - Initialization");
			
			RegisterHandlers();
		}

		private void Awake() {
			_spawnerProcesses = new RunningProcessContainer[maximumProcesses];
		}

		private void Start() {
#if UNITY_EDITOR
			processPath = editorPath;
#endif
			
			for (var i = 0; i < _spawnerProcesses.Length; i++) {
				_spawnerProcesses[i] = new RunningProcessContainer();
			}
		}

		private void RegisterHandlers() {
			if (_client) {
				_client.transport.OnClientConnected.AddListener(RegisterSpawner);
				
				_client.RegisterHandler<RequestSpawnStartToSpawnerMsg>(HandleRequestSpawnStart);
				_client.RegisterHandler<KillSpawnMsg>(HandleKillSpawn);
			}

			if (_server) {
				RegisterSpawner();
				
				_server.RegisterHandler<RequestSpawnStartToSpawnerMsg>(HandleRequestSpawnStart);
				_server.RegisterHandler<KillSpawnMsg>(HandleKillSpawn);
			}
		}

		private void HandleRequestSpawnStart(InsightMessage insightMsg) {
			var message = (RequestSpawnStartToSpawnerMsg) insightMsg.message;

			Debug.Log("[ProcessSpawner] - Received requesting game creation");

			var successful = false;
			var thisPort = GetPort();
			if (thisPort != -1) {
				//If a UniqueID was not provided add one for GameResitration
				if (string.IsNullOrEmpty(message.gameUniqueId)) {
					message.gameUniqueId = Guid.NewGuid().ToString();

					Debug.LogWarning("[ProcessSpawner] - UniqueID was not provided for spawn. Generating: " +
					                 $"{message.gameUniqueId}");
				}

				var args = ArgsString() +
				           Space + ArgNames.UniqueId + Space + message.gameUniqueId +
				           Space + ArgNames.NetworkAddress + Space + spawnerNetworkAddress +
				           Space + ArgNames.NetworkPort + Space + (startingNetworkPort + thisPort) +
				           Space + ArgNames.GameName + Space + message.gameName +
				           Space + ArgNames.MinPlayers + Space + message.minPlayers;

				var processInfo = new ProcessStartInfo {
					FileName = System.IO.Path.Combine(processPath, processName),
					Arguments = args,
					UseShellExecute = false
				};

				var process = Process.Start(processInfo);
				if (process != null) {
					Debug.Log(
						$"[ProcessSpawner] - Spawning : {process.StartInfo.FileName}; args= {process.StartInfo.Arguments}");
					process.EnableRaisingEvents = true;
					process.Exited += OnProcessExited;

					Send(new SpawnerStatusMsg {
						uniqueId = _uniqueId,
						currentThreads = GetRunningProcessCount()
					});

					_spawnerProcesses[thisPort] = new RunningProcessContainer {
						process = process,
						uniqueId = message.gameUniqueId
					};

					successful = true;
				}
			}

			if (insightMsg.callbackId != 0) {
				if (successful) {
					var requestSpawnStartMsg = new RequestSpawnStartToSpawnerMsg {
						gameUniqueId = message.gameUniqueId,
						networkAddress = spawnerNetworkAddress,
						networkPort = (ushort) (startingNetworkPort + thisPort),
					};

					var responseToSend = new InsightNetworkMessage(requestSpawnStartMsg) {
						callbackId = insightMsg.callbackId,
						status = CallbackStatus.Success
					};
					Reply(responseToSend);
				}
				else {
					var responseToSend = new InsightNetworkMessage(new RequestSpawnStartToSpawnerMsg()) {
						callbackId = insightMsg.callbackId,
						status = CallbackStatus.Error
					};
					Reply(responseToSend);
				}
			}
		}

		private void OnProcessExited(object sender, EventArgs eventArgs) {
			var process = (Process) sender;
			var spawner = _spawnerProcesses.First(e => e.process == process);
			spawner.process = null;
			spawner.uniqueId = "";

			Debug.Log("[ProcessSpawner] Removing process that has exited");
		}
		
		private void HandleKillSpawn(InsightMessage insightMsg) {
			var message = (KillSpawnMsg) insightMsg.message;

			var spawner = _spawnerProcesses.First(e => e.uniqueId == message.uniqueId);
			spawner.process.Kill();
			spawner.process = null;
			spawner.uniqueId = "";
		}

        private void RegisterSpawner() {
	        Debug.Log("[ProcessSpawner] - Registering to Master");
	        
	        Send(new RegisterSpawnerMsg {
		        maxThreads = maximumProcesses
	        }, callbackMsg => {
		        Debug.Log($"[ProcessSpawner] - Received registration : {callbackMsg.status}");
		        
		        Assert.AreNotEqual(CallbackStatus.Default, callbackMsg.status);
		        switch (callbackMsg.status) {
			        case CallbackStatus.Success: {
				        var responseReceived = (RegisterSpawnerMsg) callbackMsg.message;

				        _uniqueId = responseReceived.uniqueId;

				        break;
			        }
			        case CallbackStatus.Error:
			        case CallbackStatus.Timeout:
				        break;
			        default:
				        throw new ArgumentOutOfRangeException();
		        }
	        });
		}

        private void Send(InsightMessageBase msg, CallbackHandler callback = null) {
	        if (_client) {
		        _client.NetworkSend(msg, callback);
		        return;
	        }

	        if (_server) {
		        _server.InternalSend(msg, callback);
		        return;
	        }
	        Debug.LogError("[ProcessSpawner] - Not initialized");
        }

        private void Reply(InsightMessage insightMsg) {
	        if (_client) {
		        _client.NetworkReply((InsightNetworkMessage) insightMsg);
		        return;
	        }

	        if (_server) {
		        _server.InternalReply(insightMsg);
		        return;
	        }
	        Debug.LogError("[ProcessSpawner] - Not initialized");
        }

        private int GetPort() {
			for (var i = 0; i < _spawnerProcesses.Length; i++) {
				if (_spawnerProcesses[i].process == null) {
					return i;
				}
			}

			Debug.LogError("[ProcessSpawner] - Maximum Process Count Reached");
			return -1;
		}

        private int GetRunningProcessCount() {
			return _spawnerProcesses.Count(e => e.process != null);
        }

        private static string ArgsString() {
	        var args = Environment.GetCommandLineArgs();
	        return string.Join(" ", args.Skip(1).ToArray());
        }
	}

	[Serializable]
	public class RunningProcessContainer {
		public string uniqueId;
		public Process process;
	}
}