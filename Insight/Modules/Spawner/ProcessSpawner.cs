using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace Insight {
	public class ProcessSpawner : InsightModule {
		private InsightServer server;
		private InsightClient client;

		private string uniqueId;
		private RunningProcessContainer[] spawnerProcesses;

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

		public override void Initialize(InsightServer _server, ModuleManager _manager) {
			Debug.Log("[Server - ProcessSpawner] - Initialization");
			server = _server;

			RegisterHandlers();
			
			RegisterSpawner();
		}

		public override void Initialize(InsightClient _client, ModuleManager _manager) {
			Debug.Log("[Client - ProcessSpawner] - Initialization");
			client = _client;
			
			StartClientWith(RegisterSpawner);
			
			RegisterHandlers();
		}

		private void Awake() {
			spawnerProcesses = new RunningProcessContainer[maximumProcesses];
		}

		private void Start() {
#if UNITY_EDITOR
			processPath = editorPath;
#endif
			
			for (var i = 0; i < spawnerProcesses.Length; i++) {
				spawnerProcesses[i] = new RunningProcessContainer();
			}
		}

		private void RegisterHandlers() {
			if (client) {
				client.OnDisconnected += HandleDisconnect;
				
				client.RegisterHandler<RequestSpawnStartToSpawnerMsg>(HandleRequestSpawnStart);
				client.RegisterHandler<KillSpawnMsg>(HandleKillSpawn);
			}

			if (server) {
				server.RegisterHandler<RequestSpawnStartToSpawnerMsg>(HandleRequestSpawnStart);
				server.RegisterHandler<KillSpawnMsg>(HandleKillSpawn);
			}
		}
		
		private void StartClientWith(ConnectionDelegate _handler) {
			if (client.IsConnected) {
				_handler.Invoke();
			}
			
			client.OnConnected += _handler;
		}

		private void HandleDisconnect() {
			uniqueId = null;
			foreach (var runningProcess in spawnerProcesses) {
				runningProcess.process.Kill();
			}
		}

		private void HandleRequestSpawnStart(InsightMessage _insightMsg) {
			var message = (RequestSpawnStartToSpawnerMsg) _insightMsg.message;

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
						uniqueId = uniqueId,
						currentThreads = GetRunningProcessCount()
					});

					spawnerProcesses[thisPort] = new RunningProcessContainer {
						process = process,
						uniqueId = message.gameUniqueId
					};

					successful = true;
				}
			}

			if (_insightMsg.callbackId != 0) {
				if (successful) {
					var requestSpawnStartMsg = new RequestSpawnStartToSpawnerMsg {
						gameUniqueId = message.gameUniqueId,
						networkAddress = spawnerNetworkAddress,
						networkPort = (ushort) (startingNetworkPort + thisPort),
					};

					var responseToSend = new InsightNetworkMessage(requestSpawnStartMsg) {
						callbackId = _insightMsg.callbackId,
						status = CallbackStatus.Success
					};
					Reply(responseToSend);
				}
				else {
					var responseToSend = new InsightNetworkMessage(new RequestSpawnStartToSpawnerMsg()) {
						callbackId = _insightMsg.callbackId,
						status = CallbackStatus.Error
					};
					Reply(responseToSend);
				}
			}
		}

		private void OnProcessExited(object _sender, EventArgs _eventArgs) {
			var process = (Process) _sender;
			var spawner = spawnerProcesses.First(_e => _e.process == process);
			spawner.process = null;
			spawner.uniqueId = "";

			Debug.Log("[ProcessSpawner] Removing process that has exited");
		}
		
		private void HandleKillSpawn(InsightMessage _insightMsg) {
			var message = (KillSpawnMsg) _insightMsg.message;

			spawnerProcesses.First(_e => _e.uniqueId == message.uniqueId).process.Kill();
		}

        private void RegisterSpawner() {
	        Debug.Log("[ProcessSpawner] - Registering to Master");
	        
	        Send(new RegisterSpawnerMsg {
		        maxThreads = maximumProcesses
	        }, _callbackMsg => {
		        Debug.Log($"[ProcessSpawner] - Received registration : {_callbackMsg.status}");
		        
		        Assert.AreNotEqual(CallbackStatus.Default, _callbackMsg.status);
		        switch (_callbackMsg.status) {
			        case CallbackStatus.Success: {
				        var responseReceived = (RegisterSpawnerMsg) _callbackMsg.message;

				        uniqueId = responseReceived.uniqueId;

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

        private void Send(InsightMessageBase _message, CallbackHandler _callback = null) {
	        if (client) {
		        client.NetworkSend(_message, _callback);
		        return;
	        }

	        if (server) {
		        server.InternalSend(_message, _callback);
		        return;
	        }
	        Debug.LogError("[ProcessSpawner] - Not initialized");
        }

        private void Reply(InsightMessage _insightMsg) {
	        if (client) {
		        client.NetworkReply((InsightNetworkMessage) _insightMsg);
		        return;
	        }

	        if (server) {
		        server.InternalReply(_insightMsg);
		        return;
	        }
	        Debug.LogError("[ProcessSpawner] - Not initialized");
        }

        private int GetPort() {
			for (var i = 0; i < spawnerProcesses.Length; i++) {
				if (spawnerProcesses[i].process == null) {
					return i;
				}
			}

			Debug.LogError("[ProcessSpawner] - Maximum Process Count Reached");
			return -1;
		}

        private int GetRunningProcessCount() {
			return spawnerProcesses.Count(_e => _e.process != null);
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