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

			foreach (var game in _gameModule.registeredGameServers) {
				var playersRatioTemp = game.currentPlayers / (float) game.minPlayers;
				
				if (playersRatioTemp >= playersRatio && game.currentPlayers < game.maxPlayers) {
					playersRatio = playersRatioTemp;
					gameUniqueId = game.uniqueId;
				}
			}
			
			Assert.IsFalse(string.IsNullOrEmpty(gameUniqueId));

			return gameUniqueId;
		}
	}
}