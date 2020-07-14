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
	
	public class GameMasterManager : InsightModule {
		private readonly Dictionary<string, GameLauncher> gameLaunchers = new Dictionary<string, GameLauncher>();
		
		private InsightServer server;
		
		[HideInInspector] public List<GameContainer> registeredGameServers = new List<GameContainer>();
		[HideInInspector] public List<PlayerContainer> registeredPlayers = new List<PlayerContainer>();
		[HideInInspector] public Dictionary<string, string> playersInGame = new Dictionary<string, string>(); //<string : playerUniqueId, string : gameUniqueId>

		public void Awake() {
			AddDependency<MasterSpawner>();
		}

		public override void Initialize(InsightServer _server, ModuleManager _manager) {
			Debug.Log("[GameMaster - Manager] - Initialization");
			server = _server;

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			server.transport.OnServerDisconnected.AddListener(HandleDisconnect);
			
			server.RegisterHandler<RegisterGameMsg>(HandleRegisterGameMsg);
			server.RegisterHandler<RegisterPlayerMsg>(HandleRegisterPlayerMsg);
			server.RegisterHandler<CreateGameMsg>(HandleCreateGameMsg);
			server.RegisterHandler<GameStatusMsg>(HandleGameStatusMsg);
			server.RegisterHandler<JoinGameMsg>(HandleJoinGameMsg);
			server.RegisterHandler<LeaveGameMsg>(HandleLeaveGameMsg);
			server.RegisterHandler<GameListMsg>(HandleGameListMsg);
		}
		
		private void HandleDisconnect(int _connectionId) {
			var game = registeredGameServers.Find(_e => _e.connectionId == _connectionId);
			if (game != null) {
				registeredGameServers.Remove(game);
				foreach (var playerTemp in registeredPlayers) {
					server.NetworkSend(playerTemp.connectionId, new GameListStatusMsg {
						operation = GameListStatusMsg.Operation.Remove,
						game = game
					});
				}
				
				return;
			}

			var player = registeredPlayers.Find(_e => _e.connectionId == _connectionId);
			if (player != null) {
				registeredPlayers.Remove(player);
				
				return;
			}
		}

		private void HandleRegisterGameMsg(InsightMessage _insightMsg) {
			if (_insightMsg is InsightNetworkMessage netMsg) {
				var message = (RegisterGameMsg) _insightMsg.message;
				
				Debug.Log("[GameMaster - Manager] - Received game registration");

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
					server.NetworkSend(playerTemp.connectionId, new GameListStatusMsg {
						operation = GameListStatusMsg.Operation.Add,
						game = game
					});
				}
				
				LaunchGame(game.uniqueId);
			}
			else {
				Debug.LogError("[GameMaster - Manager] - Rejected (Internal) game registration");
			}
		}
		
		private void HandleRegisterPlayerMsg(InsightMessage _insightMsg) {
			if (_insightMsg is InsightNetworkMessage netMsg) {
				// var message = (RegisterPlayerMsg) insightMsg.message;
				
				Debug.Log("[GameMaster - Manager] - Received player registration");

				var playerUniqueId = Guid.NewGuid().ToString();
				
				registeredPlayers.Add(new PlayerContainer {
					connectionId = netMsg.connectionId,
					uniqueId = playerUniqueId
				});

				var responseToSend = new InsightNetworkMessage(
					new RegisterPlayerMsg {
						uniqueId = playerUniqueId
					}) {
					callbackId = _insightMsg.callbackId,
					status = CallbackStatus.Success
				};
				server.NetworkReply(netMsg.connectionId, responseToSend);
			}
			else {
				Debug.LogError("[GameMaster - Manager] - Rejected (Internal) player registration");
			}
		}

		private void HandleGameStatusMsg(InsightMessage _insightMsg) {
			var message = (GameStatusMsg) _insightMsg.message;

			Debug.Log("[GameMaster - Manager] - Received game update");

			var game = registeredGameServers.Find(_e => _e.uniqueId == message.uniqueId);
			Assert.IsNotNull(game);
			game.currentPlayers = message.currentPlayers;
			
			foreach (var playerTemp in registeredPlayers) {
				server.NetworkSend(playerTemp.connectionId, new GameListStatusMsg {
					operation = GameListStatusMsg.Operation.Update,
					game = game
				});
			}
		}

		private void HandleGameListMsg(InsightMessage _insightMsg) {
			// var message = (GameListMsg) insightMsg.message;

			Debug.Log("[GameMaster - Manager] - Received player requesting game list");

			var gamesListMsg = new GameListMsg();
			gamesListMsg.Load(registeredGameServers);
				
			var responseToSend = new InsightNetworkMessage(gamesListMsg) {
				callbackId = _insightMsg.callbackId,
				status = CallbackStatus.Success
			};
			
			if (_insightMsg is InsightNetworkMessage netMsg) {
				server.NetworkReply(netMsg.connectionId, responseToSend);
			}
			else {
				server.InternalReply(responseToSend);
			}
		}

		private void HandleCreateGameMsg(InsightMessage _insightMsg) {
			var message = (CreateGameMsg) _insightMsg.message;

			Debug.Log("[GameMaster - Manager] - Received player requesting game creation");

			var requestSpawnStartMsg = new RequestSpawnStartToMasterMsg {
				gameName = message.gameName,
				minPlayers = message.minPlayers
			};
				
			server.InternalSend(requestSpawnStartMsg, _callbackMsg => {
				Debug.Log($"[GameMaster - Manager] - Received games create : {_callbackMsg.status}");

				Assert.AreNotEqual(CallbackStatus.Default, _callbackMsg.status);
				switch (_callbackMsg.status) {
					case CallbackStatus.Success: {
						var responseReceived = (RequestSpawnStartMsg) _callbackMsg.message;

						var playerConnId = 0;
						if (_insightMsg is InsightNetworkMessage netMsg) playerConnId = netMsg.connectionId;
						
						gameLaunchers.Add(responseReceived.gameUniqueId, new GameLauncher {
							playerConnId = playerConnId,
							playerUniqueId = message.uniqueId,
							message = new InsightNetworkMessage(new ChangeServerMsg {
								uniqueId = responseReceived.gameUniqueId,
								networkAddress = responseReceived.networkAddress,
								networkPort = responseReceived.networkPort
							}) {
								callbackId = _insightMsg.callbackId,
								status = CallbackStatus.Success
							}
						});

						break;
					}
					case CallbackStatus.Error: {
						var responseToSend = new InsightNetworkMessage(new ChangeServerMsg ()) {
							callbackId = _insightMsg.callbackId,
							status = CallbackStatus.Error
						};
						
						if (_insightMsg is InsightNetworkMessage netMsg) {
							server.NetworkReply(netMsg.connectionId, responseToSend);
						}
						else {
							server.InternalReply(responseToSend);
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

		private void HandleJoinGameMsg(InsightMessage _insightMsg) {
			var message = (JoinGameMsg) _insightMsg.message;

			Debug.Log($"[GameMaster - Manager] - Received player requesting join the game : {message.gameUniqueId}");

			var game = registeredGameServers.Find(_e => _e.uniqueId == message.gameUniqueId);
			Assert.IsNotNull(game);

			if (game.currentPlayers < game.maxPlayers) {
				Assert.IsTrue(registeredPlayers.Exists(_e => _e.uniqueId == message.uniqueId));
				playersInGame.Add(message.uniqueId, message.gameUniqueId);

				var changeServerMsg = new ChangeServerMsg {
					uniqueId = game.uniqueId,
					networkAddress = game.networkAddress,
					networkPort = game.networkPort
				};
					
				var responseToSend = new InsightNetworkMessage(changeServerMsg) {
					callbackId = _insightMsg.callbackId,
					status = CallbackStatus.Success
				};
				
				if (_insightMsg is InsightNetworkMessage netMsg) {
					server.NetworkReply(netMsg.connectionId, responseToSend);
				}
				else {
					server.InternalReply(responseToSend);
				}
			}
			else {
				var responseToSend = new InsightNetworkMessage(new ChangeServerMsg()) {
					callbackId = _insightMsg.callbackId,
					status = CallbackStatus.Error
				};
				
				if (_insightMsg is InsightNetworkMessage netMsg) {
					server.NetworkReply(netMsg.connectionId, responseToSend);
				}
				else {
					server.InternalReply(responseToSend);
				}
			}
		}

		private void HandleLeaveGameMsg(InsightMessage _insightMsg) {
			var message = (LeaveGameMsg) _insightMsg.message;

			Debug.Log("[GameMaster - Manager] - Received player requesting leave the game ");

			playersInGame.Remove(message.uniqueId);
		}

		private void LaunchGame(string _gameUniqueId) {
			var gameLauncher = gameLaunchers[_gameUniqueId];

			if (gameLauncher.playerConnId != 0) {
				server.NetworkReply(gameLauncher.playerConnId, gameLauncher.message);
			}
			else {
				server.InternalReply(gameLauncher.message);
			}
			
			Assert.IsTrue(registeredPlayers.Exists(_e => _e.uniqueId == gameLauncher.playerUniqueId));
			playersInGame.Add(gameLauncher.playerUniqueId, _gameUniqueId);

			gameLaunchers.Remove(_gameUniqueId);
		}

		public IEnumerable<int> GetPlayersInGame(int _playerConnId) {
			var player = registeredPlayers.Find(_e => _e.connectionId == _playerConnId);
			return player == null ? null : GetPlayersInGame(playersInGame[player.uniqueId]);
		}

		private int[] GetPlayersInGame(string _gameUniqueId) {
			return (from playerInGame in playersInGame.Where(_e => _e.Value == _gameUniqueId)
				from player in registeredPlayers.FindAll(_e => _e.uniqueId == playerInGame.Key)
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