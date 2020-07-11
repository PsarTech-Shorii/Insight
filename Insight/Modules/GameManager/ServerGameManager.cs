using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public struct GameLauncher {
		public int playerConnId;
		public string playerUniqueId;
		public InsightNetworkMessage message;
	}
	
	public class ServerGameManager : InsightModule {
		private InsightServer _server;

		private readonly Dictionary<string, GameLauncher> _gameLaunchers = new Dictionary<string, GameLauncher>();
		
		public List<GameContainer> registeredGameServers = new List<GameContainer>();
		public List<PlayerContainer> registeredPlayers = new List<PlayerContainer>();
		public Dictionary<string, string> playersInGame = new Dictionary<string, string>(); //<string : playerUniqueId, string : gameUniqueId>

		public void Awake() {
			AddDependency<MasterSpawner>();
		}

		public override void Initialize(InsightServer server, ModuleManager manager) {
			_server = server;
			
			Debug.Log("[Server - GameManager] - Initialization");
			
			RegisterHandlers();
		}

		private void RegisterHandlers() {
			_server.transport.OnServerDisconnected.AddListener(HandleDisconnect);
			
			_server.RegisterHandler<RegisterGameMsg>(HandleRegisterGameMsg);
			_server.RegisterHandler<RegisterPlayerMsg>(HandleRegisterPlayerMsg);
			_server.RegisterHandler<CreateGameMsg>(HandleCreateGameMsg);
			_server.RegisterHandler<GameStatusMsg>(HandleGameStatusMsg);
			_server.RegisterHandler<JoinGameMsg>(HandleJoinGameMsg);
			_server.RegisterHandler<LeaveGameMsg>(HandleLeaveGameMsg);
			_server.RegisterHandler<GameListMsg>(HandleGameListMsg);
		}
		
		private void HandleDisconnect(int connectionId) {
			var game = registeredGameServers.Find(e => e.connectionId == connectionId);
			if (game != null) {
				registeredGameServers.Remove(game);
				foreach (var playerTemp in registeredPlayers) {
					_server.NetworkSend(playerTemp.connectionId, new GameListStatusMsg {
						operation = GameListStatusMsg.Operation.Remove,
						game = game
					});
				}
				
				return;
			}

			var player = registeredPlayers.Find(e => e.connectionId == connectionId);
			if (player != null) {
				registeredPlayers.Remove(player);
				
				return;
			}
		}

		private void HandleRegisterGameMsg(InsightMessage insightMsg) {
			if (insightMsg is InsightNetworkMessage netMsg) {
				var message = (RegisterGameMsg) insightMsg.message;
				
				Debug.Log("[Server - GameManager] - Received game registration");

				var game = new GameContainer {
					connectionId = netMsg.connectionId,

					uniqueId = message.uniqueId,
					networkAddress = message.networkAddress,
					networkPort = message.networkPort,
					gameName = message.gameName,
					minPlayers = message.minPlayers,
					maxPlayers = message.maxPlayers,
					currentPlayers = message.currentPlayers,
				};

				registeredGameServers.Add(game);

				foreach (var playerTemp in registeredPlayers) {
					_server.NetworkSend(playerTemp.connectionId, new GameListStatusMsg {
						operation = GameListStatusMsg.Operation.Add,
						game = game
					});
				}
				
				LaunchGame(game.uniqueId);
			}
			else {
				Debug.LogError("[Server - GameManager] - Rejected (Internal) game registration");
			}
		}
		
		private void HandleRegisterPlayerMsg(InsightMessage insightMsg) {
			if (insightMsg is InsightNetworkMessage netMsg) {
				// var message = (RegisterPlayerMsg) insightMsg.message;
				
				Debug.Log("[Server - GameManager] - Received player registration");

				var playerUniqueId = Guid.NewGuid().ToString();
				
				registeredPlayers.Add(new PlayerContainer {
					connectionId = netMsg.connectionId,
					uniqueId = playerUniqueId
				});

				var responseToSend = new InsightNetworkMessage(
					new RegisterPlayerMsg {
						uniqueId = playerUniqueId
					}) {
					callbackId = insightMsg.callbackId,
					status = CallbackStatus.Success
				};
				_server.NetworkReply(netMsg.connectionId, responseToSend);
			}
			else {
				Debug.LogError("[Server - GameManager] - Rejected (Internal) player registration");
			}
		}

		private void HandleGameStatusMsg(InsightMessage insightMsg) {
			var message = (GameStatusMsg) insightMsg.message;

			Debug.Log("[Server - GameManager] - Received game update");

			var game = registeredGameServers.Find(e => e.uniqueId == message.uniqueId);
			Assert.IsNotNull(game);
			game.currentPlayers = message.currentPlayers;
			
			foreach (var playerTemp in registeredPlayers) {
				_server.NetworkSend(playerTemp.connectionId, new GameListStatusMsg {
					operation = GameListStatusMsg.Operation.Update,
					game = game
				});
			}
		}

		private void HandleGameListMsg(InsightMessage insightMsg) {
			// var message = (GameListMsg) insightMsg.message;

			Debug.Log("[Server - GameManager] - Received player requesting game list");

			var gamesListMsg = new GameListMsg();
			gamesListMsg.Load(registeredGameServers);
				
			var responseToSend = new InsightNetworkMessage(gamesListMsg) {
				callbackId = insightMsg.callbackId,
				status = CallbackStatus.Success
			};
			
			if (insightMsg is InsightNetworkMessage netMsg) {
				_server.NetworkReply(netMsg.connectionId, responseToSend);
			}
			else {
				_server.InternalReply(responseToSend);
			}
		}

		private void HandleCreateGameMsg(InsightMessage insightMsg) {
			var message = (CreateGameMsg) insightMsg.message;

			Debug.Log("[Server - GameManager] - Received player requesting game creation");

			var requestSpawnStartMsg = new RequestSpawnStartToMasterMsg {
				gameName = message.gameName,
				minPlayers = message.minPlayers
			};
				
			_server.InternalSend(requestSpawnStartMsg, callbackMsg => {
				Debug.Log($"[Server - GameManager] - Received games create : {callbackMsg.status}");

				Assert.AreNotEqual(CallbackStatus.Default, callbackMsg.status);
				switch (callbackMsg.status) {
					case CallbackStatus.Success: {
						var responseReceived = (RequestSpawnStartMsg) callbackMsg.message;

						var playerConnId = 0;
						if (insightMsg is InsightNetworkMessage netMsg) playerConnId = netMsg.connectionId;
						
						_gameLaunchers.Add(responseReceived.gameUniqueId, new GameLauncher {
							playerConnId = playerConnId,
							playerUniqueId = message.uniqueId,
							message = new InsightNetworkMessage(new ChangeServerMsg {
								uniqueId = responseReceived.gameUniqueId,
								networkAddress = responseReceived.networkAddress,
								networkPort = responseReceived.networkPort
							}) {
								callbackId = insightMsg.callbackId,
								status = CallbackStatus.Success
							}
						});

						break;
					}
					case CallbackStatus.Error: {
						var responseToSend = new InsightNetworkMessage(new ChangeServerMsg ()) {
							callbackId = insightMsg.callbackId,
							status = CallbackStatus.Error
						};
						
						if (insightMsg is InsightNetworkMessage netMsg) {
							_server.NetworkReply(netMsg.connectionId, responseToSend);
						}
						else {
							_server.InternalReply(responseToSend);
						}
							
						break;
					}
					case CallbackStatus.Timeout:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			});
		}

		private void HandleJoinGameMsg(InsightMessage insightMsg) {
			var message = (JoinGameMsg) insightMsg.message;

			Debug.Log($"[Server - GameManager] - Received player requesting join the game : {message.gameUniqueId}");

			var game = registeredGameServers.Find(e => e.uniqueId == message.gameUniqueId);
			Assert.IsNotNull(game);

			if (game.currentPlayers < game.maxPlayers) {
				Assert.IsTrue(registeredPlayers.Exists(e => e.uniqueId == message.uniqueId));
				playersInGame.Add(message.uniqueId, message.gameUniqueId);

				var changeServerMsg = new ChangeServerMsg {
					uniqueId = game.uniqueId,
					networkAddress = game.networkAddress,
					networkPort = game.networkPort
				};
					
				var responseToSend = new InsightNetworkMessage(changeServerMsg) {
					callbackId = insightMsg.callbackId,
					status = CallbackStatus.Success
				};
				
				if (insightMsg is InsightNetworkMessage netMsg) {
					_server.NetworkReply(netMsg.connectionId, responseToSend);
				}
				else {
					_server.InternalReply(responseToSend);
				}
			}
			else {
				var responseToSend = new InsightNetworkMessage(new ChangeServerMsg()) {
					callbackId = insightMsg.callbackId,
					status = CallbackStatus.Error
				};
				
				if (insightMsg is InsightNetworkMessage netMsg) {
					_server.NetworkReply(netMsg.connectionId, responseToSend);
				}
				else {
					_server.InternalReply(responseToSend);
				}
			}
		}

		private void HandleLeaveGameMsg(InsightMessage insightMsg) {
			var message = (LeaveGameMsg) insightMsg.message;

			Debug.Log("[Server - GameManager] - Received player requesting leave the game ");

			playersInGame.Remove(message.uniqueId);
		}

		private void LaunchGame(string gameUniqueId) {
			var gameLauncher = _gameLaunchers[gameUniqueId];

			if (gameLauncher.playerConnId != 0) {
				_server.NetworkReply(gameLauncher.playerConnId, gameLauncher.message);
			}
			else {
				_server.InternalReply(gameLauncher.message);
			}
			
			Assert.IsTrue(registeredPlayers.Exists(e => e.uniqueId == gameLauncher.playerUniqueId));
			playersInGame.Add(gameLauncher.playerUniqueId, gameUniqueId);

			_gameLaunchers.Remove(gameUniqueId);
		}

		public IEnumerable<int> GetPlayersInGame(int playerConnId) {
			var player = registeredPlayers.Find(e => e.connectionId == playerConnId);
			return player == null ? null : GetPlayersInGame(playersInGame[player.uniqueId]);
		}

		private int[] GetPlayersInGame(string gameUniqueId) {
			return (from playerInGame in playersInGame.Where(e => e.Value == gameUniqueId)
				from player in registeredPlayers.FindAll(e => e.uniqueId == playerInGame.Key)
				select player.connectionId).ToArray();
		}
	}

	[Serializable]
	public class GameContainer {
		public int connectionId;
		public string uniqueId;
		public string networkAddress;
		public ushort networkPort;
		
		public string gameName;
		public int minPlayers;
		public int maxPlayers;
		public int currentPlayers;
		public bool hasStarted;
	}
	
	[Serializable]
	public class PlayerContainer {
		public int connectionId;
		public string uniqueId;
	}
}