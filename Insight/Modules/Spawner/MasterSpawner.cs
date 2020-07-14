using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class MasterSpawner : InsightModule {
		private InsightServer server;
		
		[HideInInspector] public List<SpawnerContainer> registeredSpawners = new List<SpawnerContainer>();

		public override void Initialize(InsightServer _server, ModuleManager _manager) {
			Debug.Log("[MasterSpawner] - Initialization");
			server = _server;

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			server.transport.OnServerDisconnected.AddListener(HandleDisconnect);

			server.RegisterHandler<RegisterSpawnerMsg>(HandleRegisterSpawnerMsg);
			server.RegisterHandler<RequestSpawnStartToMasterMsg>(HandleSpawnRequestMsg);
			server.RegisterHandler<SpawnerStatusMsg>(HandleSpawnerStatusMsg);
		}

		private void HandleDisconnect(int _connectionId) {
			registeredSpawners.Remove(registeredSpawners.Find(_e => _e.connectionId == _connectionId));
		}

		private void HandleRegisterSpawnerMsg(InsightMessage _insightMsg) {
			var message = (RegisterSpawnerMsg) _insightMsg.message;
				
			Debug.Log("[MasterSpawner] - Received process spawner registration");

			var connectionId = _insightMsg is InsightNetworkMessage netMsg ? netMsg.connectionId : 0;
			var uniqueId = Guid.NewGuid().ToString();
			var responseToSend = new InsightNetworkMessage(
				new RegisterSpawnerMsg {
					uniqueId = uniqueId
				}) {
				callbackId = _insightMsg.callbackId,
				status = CallbackStatus.Success
			};
			ReplyToSpawner(connectionId, responseToSend);
			
			registeredSpawners.Add(new SpawnerContainer {
				connectionId = connectionId,
				uniqueId = uniqueId,
				maxThreads = message.maxThreads
			});
		}

		//Instead of handling the msg here we will forward it to an available spawner.
		private void HandleSpawnRequestMsg(InsightMessage _insightMsg) {
			if (registeredSpawners.Count == 0) {
				Debug.LogWarning("[MasterSpawner] - No spawner regsitered to handle spawn request");
				return;
			}

			var message = (RequestSpawnStartToMasterMsg) _insightMsg.message;

			Debug.Log("[MasterSpawner] - Received requesting game creation");

			//Get all spawners that have atleast 1 slot free
			var freeSlotSpawners = registeredSpawners.FindAll(_e => _e.currentThreads < _e.maxThreads);

			//sort by least busy spawner first
			freeSlotSpawners = freeSlotSpawners.OrderBy(_e => _e.currentThreads).ToList();
			SendToSpawner(freeSlotSpawners[0].connectionId, message, _callbackMsg => {
				Debug.Log($"[MasterSpawner] - Game creation on child spawner : {_callbackMsg.status}");

				if (_insightMsg.callbackId != 0) {
					var responseToSend = new InsightNetworkMessage(_callbackMsg) {
						callbackId = _insightMsg.callbackId
					};

					if (_insightMsg is InsightNetworkMessage netMsg) {
						server.NetworkReply(netMsg.connectionId, responseToSend);
					}
					else {
						server.InternalReply(responseToSend);
					}
				}
			});
		}

		private void HandleSpawnerStatusMsg(InsightMessage _insightMsg) {
			var message = (SpawnerStatusMsg) _insightMsg.message;

			Debug.Log("Received process spawner update");
			
			var spawner = registeredSpawners.Find(_e => _e.uniqueId == message.uniqueId);
			Assert.IsNotNull(spawner);
			spawner.currentThreads = message.currentThreads;
		}

		private void SendToSpawner(int _connectionId, RequestSpawnStartMsg _message, CallbackHandler _callback = null) {
			var message = new RequestSpawnStartToSpawnerMsg(_message);
			if(_connectionId == 0) server.InternalSend(message, _callback);
			else server.NetworkSend(_connectionId, message, _callback);
		}

		private void ReplyToSpawner(int _connectionId, InsightNetworkMessage _netMsg) {
			if(_connectionId == 0) server.InternalReply(_netMsg);
			else server.NetworkReply(_connectionId, _netMsg);
		}
	}

	[Serializable]
	public class SpawnerContainer {
		public int connectionId;
		public string uniqueId;
		public int maxThreads;
		public int currentThreads;
	}
}