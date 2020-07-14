using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class ClientMatchMaker : InsightModule {
		private InsightClient client;
		private GameClientManager gameModule;

		private void Awake() {
			AddDependency<GameClientManager>();
		}
		
		public override void Initialize(InsightClient _client, ModuleManager _manager) {
			Debug.Log("[Client - MatchMaker] - Initialization");
			client = _client;
			
			gameModule = _manager.GetModule<GameClientManager>();
		}

		public void MatchGame() {
			Assert.IsTrue(client.IsConnected);
			Debug.Log("[Client - MatchMaker] - Match game"); 
			
			client.NetworkSend(new MatchGameMsg {uniqueId = gameModule.uniqueId}, 
				_callbackMsg => client.InternalSend(_callbackMsg.message));
		}
	}
}