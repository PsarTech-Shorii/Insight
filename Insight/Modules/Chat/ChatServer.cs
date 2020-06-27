using UnityEngine;

namespace Insight {
	public class ChatServer : InsightModule {
		private InsightServer _server;
		private ServerAuthentication _authModule;
		private ServerGameManager _gameModule;
		

		public void Awake() {
			AddDependency<ServerAuthentication>();
			AddOptionalDependency<ServerGameManager>();
		}

		public override void Initialize(InsightServer server, ModuleManager manager) {
			_server = server;

			_authModule = manager.GetModule<ServerAuthentication>();
			_gameModule = manager.GetModule<ServerGameManager>();

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			_server.RegisterHandler<ChatMsg>(HandleChatMsg);
		}

		private void HandleChatMsg(InsightMessage insightMsg) {
			if (insightMsg is InsightNetworkMessage netMsg) {
				var message = (ChatMsg) insightMsg.message;
				
				Debug.Log("[ChatServer] - Received Chat Message.");

				//Inject the username into the message
				message.username = _authModule.registeredUsers.Find(e => e.connectionId == netMsg.connectionId)
					.username;

				if (_gameModule != null) {
					foreach (var playerConnId in _gameModule.GetPlayersInGame(netMsg.connectionId)) {
						_server.NetworkSend(playerConnId, message);
					}
				}
				else {
					foreach (var user in _authModule.registeredUsers) {
						_server.NetworkSend(user.connectionId, message);
					}
				}
			}
			else {
				Debug.Log("[ChatServer] - Rejected (Internal) Chat Message.");
			}
		}
	}
}