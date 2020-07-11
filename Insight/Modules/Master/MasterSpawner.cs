using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class MasterSpawner : InsightModule {
		private InsightServer _server;

		[HideInInspector] public List<SpawnerContainer> registeredSpawners = new List<SpawnerContainer>();

		public override void Initialize(InsightServer server, ModuleManager manager) {
			_server = server;

			Debug.Log("[MasterSpawner] - Initialization");

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			_server.transport.OnServerDisconnected.AddListener(HandleDisconnect);

			_server.RegisterHandler<RegisterSpawnerMsg>(HandleRegisterSpawnerMsg);
			_server.RegisterHandler<RequestSpawnStartToMasterMsg>(HandleSpawnRequestMsg);
			_server.RegisterHandler<SpawnerStatusMsg>(HandleSpawnerStatusMsg);
		}

		private void HandleDisconnect(int connectionId) {
			registeredSpawners.Remove(registeredSpawners.Find(e => e.connectionId == connectionId));
		}

		private void HandleRegisterSpawnerMsg(InsightMessage insightMsg) {
			var message = (RegisterSpawnerMsg) insightMsg.message;
				
			Debug.Log("[MasterSpawner] - Received process spawner registration");

			var connectionId = insightMsg is InsightNetworkMessage netMsg ? netMsg.connectionId : 0;
			var uniqueId = Guid.NewGuid().ToString();
			var responseToSend = new InsightNetworkMessage(
				new RegisterSpawnerMsg {
					uniqueId = uniqueId
				}) {
				callbackId = insightMsg.callbackId,
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
		private void HandleSpawnRequestMsg(InsightMessage insightMsg) {
			if (registeredSpawners.Count == 0) {
				Debug.LogWarning("[MasterSpawner] - No spawner regsitered to handle spawn request");
				return;
			}

			var message = (RequestSpawnStartToMasterMsg) insightMsg.message;

			Debug.Log("[MasterSpawner] - Received requesting game creation");

			//Get all spawners that have atleast 1 slot free
			var freeSlotSpawners = registeredSpawners.FindAll(e => e.currentThreads < e.maxThreads);

			//sort by least busy spawner first
			freeSlotSpawners = freeSlotSpawners.OrderBy(x => x.currentThreads).ToList();
			SendToSpawner(freeSlotSpawners[0].connectionId, message, callbackMsg => {
				Debug.Log($"[MasterSpawner] - Game creation on child spawner : {callbackMsg.status}");

				if (insightMsg.callbackId != 0) {
					var responseToSend = new InsightNetworkMessage(callbackMsg) {
						callbackId = insightMsg.callbackId
					};

					if (insightMsg is InsightNetworkMessage netMsg) {
						_server.NetworkReply(netMsg.connectionId, responseToSend);
					}
					else {
						_server.InternalReply(responseToSend);
					}
				}
			});
		}

		private void HandleSpawnerStatusMsg(InsightMessage insightMsg) {
			var message = (SpawnerStatusMsg) insightMsg.message;

			Debug.Log("Received process spawner update");
			
			var spawner = registeredSpawners.Find(e => e.uniqueId == message.uniqueId);
			Assert.IsNotNull(spawner);
			spawner.currentThreads = message.currentThreads;
		}

		private void SendToSpawner(int connectionId, RequestSpawnStartMsg msg, CallbackHandler callback = null) {
			var message = new RequestSpawnStartToSpawnerMsg(msg);
			if(connectionId == 0) _server.InternalSend(message, callback);
			else _server.NetworkSend(connectionId, message, callback);
		}

		private void ReplyToSpawner(int connectionId, InsightNetworkMessage netMsg) {
			if(connectionId == 0) _server.InternalReply(netMsg);
			else _server.NetworkReply(connectionId, netMsg);
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