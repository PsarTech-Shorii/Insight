using UnityEngine;

namespace Insight {
	public class ChatServer : InsightModule {
		private InsightServer server;
		private ServerAuthentication authModule;
		private GameMasterManager gameModule;
		

		public void Awake() {
			AddDependency<ServerAuthentication>();
			AddOptionalDependency<GameMasterManager>();
		}

		public override void Initialize(InsightServer _server, ModuleManager _manager) {
			Debug.Log("[ChatServer] - Initialization");
			server = _server;

			authModule = _manager.GetModule<ServerAuthentication>();
			gameModule = _manager.GetModule<GameMasterManager>();

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			server.RegisterHandler<ChatMsg>(HandleChatMsg);
		}

		private void HandleChatMsg(InsightMessage _insightMsg) {
			if (_insightMsg is InsightNetworkMessage netMsg) {
				var message = (ChatMsg) _insightMsg.message;
				
				Debug.Log("[ChatServer] - Received Chat Message.");

				//Inject the username into the message
				message.username = authModule.registeredUsers.Find(_e => _e.connectionId == netMsg.connectionId)
					.username;

				if (gameModule != null) {
					foreach (var playerConnId in gameModule.GetPlayersInGame(netMsg.connectionId)) {
						server.NetworkSend(playerConnId, message);
					}
				}
				else {
					foreach (var user in authModule.registeredUsers) {
						server.NetworkSend(user.connectionId, message);
					}
				}
			}
			else {
				Debug.Log("[ChatServer] - Rejected (Internal) Chat Message.");
			}
		}
	}
}