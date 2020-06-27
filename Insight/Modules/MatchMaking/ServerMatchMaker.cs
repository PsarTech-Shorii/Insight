using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class ServerMatchMaker : InsightModule {
		private InsightServer _server;
		private ServerGameManager _gameModule;

		private void Awake() {
			AddDependency<ServerGameManager>();
		}

		public override void Initialize(InsightServer server, ModuleManager manager) {
			_server = server;
			
			_gameModule = manager.GetModule<ServerGameManager>();
			
			RegisterHandlers();
		}

		private void RegisterHandlers() {
			_server.RegisterHandler<MatchGameMsg>(HandleMatchGameMsg);
		}

		private void HandleMatchGameMsg(InsightMessage insightMsg) {
			var message = (MatchGameMsg) insightMsg.message;
			
			Debug.Log("[Server - MatchMaker] - Received requesting match game");
			
			_server.InternalSend(new JoinGameMsg {
				uniqueId = message.uniqueId,
				gameUniqueId = GetFastestGame()
			}, callbackMsg => {
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

		private string GetFastestGame() {
			var playersRatio = 0f;
			var gameUniqueId = "";

			Assert.IsFalse(_gameModule.registeredGameServers.Count == 0);
			
			foreach (var game in _gameModule.registeredGameServers) {
				if (game.currentPlayers / (float) game.minPlayers >= playersRatio) {
					playersRatio = game.currentPlayers / (float) game.minPlayers;
					gameUniqueId = game.uniqueId;
				}
			}

			return gameUniqueId;
		}
	}
}