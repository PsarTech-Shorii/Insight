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
			_server.RegisterHandler<RequestSpawnStartMsg>(HandleSpawnRequestMsg);
			_server.RegisterHandler<SpawnerStatusMsg>(HandleSpawnerStatusMsg);
		}

		private void HandleDisconnect(int connectionId) {
			registeredSpawners.Remove(registeredSpawners.Find(e => e.connectionId == connectionId));
		}

		private void HandleRegisterSpawnerMsg(InsightMessage insightMsg) {
			if (insightMsg is InsightNetworkMessage netMsg) {
				var message = (RegisterSpawnerMsg) insightMsg.message;
				
				Debug.Log("[MasterSpawner] - Received process spawner registration");

				registeredSpawners.Add(new SpawnerContainer {
					connectionId = netMsg.connectionId,
					uniqueId = message.uniqueId,
					maxThreads = message.maxThreads
				});
			}
			else {
				Debug.Log("[MasterSpawner] - Rejected (Internal) process spawner registration");
			}
		}

		//Instead of handling the msg here we will forward it to an available spawner.
		private void HandleSpawnRequestMsg(InsightMessage insightMsg) {
			if (registeredSpawners.Count == 0) {
				Debug.LogWarning("[MasterSpawner] - No spawner regsitered to handle spawn request");
				return;
			}

			var message = (RequestSpawnStartMsg) insightMsg.message;
			message.spawnerUniqueId = Guid.NewGuid().ToString();
			
			Debug.Log("[MasterSpawner] - Received requesting game creation");

			//Get all spawners that have atleast 1 slot free
			var freeSlotSpawners = registeredSpawners.FindAll(e => e.currentThreads < e.maxThreads);

			//sort by least busy spawner first
			freeSlotSpawners = freeSlotSpawners.OrderBy(x => x.currentThreads).ToList();
			_server.NetworkSend(freeSlotSpawners[0].connectionId, message, callbackMsg => {
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
	}

	[Serializable]
	public class SpawnerContainer {
		public int connectionId;
		public string uniqueId;
		public int maxThreads;
		public int currentThreads;
	}
}