using System;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class GameClientManager : InsightModule {
		public delegate void GoInGame(bool _newValue);
		
		private InsightClient client;
		private NetworkManager netMananager;
		private Transport transport;

		private bool isInGame;

		[HideInInspector] public string uniqueId;
		[HideInInspector] public List<GameContainer> gamesList = new List<GameContainer>();

		public event GoInGame OnGoInGame;
		public bool IsInGame {
			get => isInGame;
			private set {
				isInGame = value;
				OnGoInGame?.Invoke(isInGame);
			}
		}

		public override void Initialize(InsightClient _client, ModuleManager _manager) {
			Debug.Log("[GameClientManager] - Initialization");
			client = _client;
			netMananager = NetworkManager.singleton;
			transport = Transport.activeTransport;

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			client.OnConnected += RegisterPlayer;
			client.OnConnected += GetGameList;
			client.OnDisconnected += HandleDisconnect;

			client.RegisterHandler<ChangeServerMsg>(HandleChangeServersMsg);
			client.RegisterHandler<GameListStatusMsg>(HandleGameListStatutMsg);
			
			transport.OnClientDisconnected.AddListener(() => IsInGame = false);

			OnGoInGame += _newValue => {
				if (_newValue) netMananager.StartClient();
				else netMananager.StopClient();
			};
		}

		#region Handler

		private void HandleDisconnect() {
			uniqueId = null;
			gamesList.Clear();
			IsInGame = false;
		}
		
		private void HandleChangeServersMsg(InsightMessage _insightMsg) {
			Debug.Log("[GameClientManager] - Connection to GameServer" +
			          (_insightMsg.status == CallbackStatus.Default ? "" : $" : {_insightMsg.status}"));

			switch (_insightMsg.status) {
				case CallbackStatus.Default:
				case CallbackStatus.Success: {
					var responseReceived = (ChangeServerMsg) _insightMsg.message;
					if (transport.GetType().GetField("port") != null) {
						transport.GetType().GetField("port")
							.SetValue(transport, responseReceived.networkPort);
					}

					netMananager.networkAddress = responseReceived.networkAddress;
					IsInGame = true;
					
					break;
				}
				case CallbackStatus.Error:
				case CallbackStatus.Timeout:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (_insightMsg.status == CallbackStatus.Default) {
				ReceiveMessage(_insightMsg.message);
			}
			else {
				ReceiveResponse(_insightMsg.message, _insightMsg.status);
			}
		}

		private void HandleGameListStatutMsg(InsightMessage _insightMsg) {
			var message = (GameListStatusMsg) _insightMsg.message;
			
			Debug.Log("[GameClientManager] - Received games list update");

			switch (message.operation) {
				case GameListStatusMsg.Operation.Add:
					gamesList.Add(message.game);
					break;
				case GameListStatusMsg.Operation.Remove:
					gamesList.Remove(gamesList.Find(_game => _game.uniqueId == message.game.uniqueId));
					break;
				case GameListStatusMsg.Operation.Update:
					var gameTemp = gamesList.Find(_game => _game.uniqueId == message.game.uniqueId);
					gameTemp.Update(message.game);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			ReceiveMessage(message);
		}

		#endregion

		#region Sender

		private void RegisterPlayer() {
			Assert.IsTrue(client.IsConnected);
			Debug.Log("[GameClientManager] - Registering player"); 
			client.NetworkSend(new RegisterPlayerMsg(), _callbackMsg => {
				Debug.Log($"[GameClientManager] - Received registration : {_callbackMsg.status}");

				Assert.AreNotEqual(CallbackStatus.Default, _callbackMsg.status);
				switch (_callbackMsg.status) {
					case CallbackStatus.Success: {
						var responseReceived = (RegisterPlayerMsg) _callbackMsg.message;

						uniqueId = responseReceived.uniqueId;

						break;
					}
					case CallbackStatus.Error:
					case CallbackStatus.Timeout:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			});
		}

		private void GetGameList() {
			Assert.IsTrue(client.IsConnected);
			Debug.Log("[GameClientManager] - Getting game list");
			
			client.NetworkSend(new GameListMsg(), _callbackMsg => {
				Debug.Log($"[GameClientManager] - Received games list : {_callbackMsg.status}");
				
				Assert.AreNotEqual(CallbackStatus.Default, _callbackMsg.status);
				switch (_callbackMsg.status) {
					case CallbackStatus.Success: {
						var responseReceived = (GameListMsg) _callbackMsg.message;
						
						gamesList.Clear();

						foreach (var game in responseReceived.gamesArray) {
							gamesList.Add(game);
						}
						break;
					}
					case CallbackStatus.Error:
					case CallbackStatus.Timeout:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				
				ReceiveResponse(_callbackMsg.message, _callbackMsg.status);
			});
		}

		public void CreateGame(CreateGameMsg _createGameMsg) {
			Assert.IsFalse(IsInGame);
			Assert.IsTrue(client.IsConnected);
			Debug.Log("[GameClientManager] - Creating game ");
			_createGameMsg.uniqueId = uniqueId;
			client.NetworkSend(_createGameMsg, HandleChangeServersMsg);
		}
		
		public void JoinGame(string _gameUniqueId) {
			Assert.IsFalse(IsInGame);
			Assert.IsTrue(client.IsConnected);
			Debug.Log("[GameClientManager] - Joining game : " + _gameUniqueId);

			client.NetworkSend(new JoinGameMsg {
				uniqueId = uniqueId,
				gameUniqueId = _gameUniqueId
			}, HandleChangeServersMsg);
		}

		public void LeaveGame() {
			Assert.IsTrue(IsInGame);
			Assert.IsTrue(client.IsConnected);
			Debug.Log("[GameClientManager] - Leaving game"); 
			
			client.NetworkSend(new LeaveGameMsg{uniqueId = uniqueId});
			IsInGame = false;
		}

		#endregion
	}
}