using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class ServerMatchMaker : InsightModule {
		private InsightServer server;
		private GameMasterManager gameModule;

		private void Awake() {
			AddDependency<GameMasterManager>();
		}

		public override void Initialize(InsightServer _server, ModuleManager _manager) {
			Debug.Log("[Server - MatchMaker] - Initialization");
			server = _server;
			
			gameModule = _manager.GetModule<GameMasterManager>();
			
			RegisterHandlers();
		}

		private void RegisterHandlers() {
			server.RegisterHandler<MatchGameMsg>(HandleMatchGameMsg);
		}

		private void HandleMatchGameMsg(InsightMessage _insightMsg) {
			var message = (MatchGameMsg) _insightMsg.message;
			
			Debug.Log("[Server - MatchMaker] - Received requesting match game");
			
			server.InternalSend(new JoinGameMsg {
				uniqueId = message.uniqueId,
				gameUniqueId = GetFastestGame()
			}, _callbackMsg => {
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

		private string GetFastestGame() {
			var playersRatio = 0f;
			var gameUniqueId = "";

			foreach (var game in gameModule.registeredGameServers) {
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