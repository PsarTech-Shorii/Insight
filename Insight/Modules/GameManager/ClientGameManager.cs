using System;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class ClientGameManager : InsightModule {
		public delegate void GoInGame(bool newValue);
		
		private InsightClient _client;
		private NetworkManager _netMananager;
		private Transport _transport;

		private bool _isInGame;

		[HideInInspector] public string uniqueId;
		[HideInInspector] public List<GameContainer> gamesList = new List<GameContainer>();

		public event GoInGame OnGoInGame;
		public bool IsInGame {
			get => _isInGame;
			private set {
				_isInGame = value;
				OnGoInGame?.Invoke(_isInGame);
			}
		}

		public override void Initialize(InsightClient client, ModuleManager manager) {
			Debug.Log("[Client - GameManager] - Initialization");
			
			_client = client;
			_netMananager = NetworkManager.singleton;
			_transport = Transport.activeTransport;

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			_client.transport.OnClientConnected.AddListener(RegisterPlayer);
			_client.transport.OnClientConnected.AddListener(GetGameList);

			_client.RegisterHandler<ChangeServerMsg>(HandleChangeServersMsg);
			_client.RegisterHandler<GameListStatusMsg>(HandleGameListStatutMsg);
		}

		#region Handler

		private void HandleChangeServersMsg(InsightMessage insightMsg) {
			Debug.Log("[Client - GameManager] - Connection to GameServer" +
			          (insightMsg.status == CallbackStatus.Default ? "" : $" : {insightMsg.status}"));

			switch (insightMsg.status) {
				case CallbackStatus.Default:
				case CallbackStatus.Success: {
					var responseReceived = (ChangeServerMsg) insightMsg.message;
					if (_transport.GetType().GetField("port") != null) {
						_transport.GetType().GetField("port")
							.SetValue(_transport, responseReceived.networkPort);
					}

					IsInGame = true;

					_netMananager.networkAddress = responseReceived.networkAddress;
					_netMananager.StartClient();
					break;
				}
				case CallbackStatus.Error:
				case CallbackStatus.Timeout:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (insightMsg.status == CallbackStatus.Default) {
				ReceiveMessage(insightMsg.message);
			}
			else {
				ReceiveResponse(insightMsg.message, insightMsg.status);
			}
		}

		private void HandleGameListStatutMsg(InsightMessage insightMsg) {
			var message = (GameListStatusMsg) insightMsg.message;
			
			Debug.Log("[Client - GameManager] - Received games list update");

			switch (message.operation) {
				case GameListStatusMsg.Operation.Add:
					gamesList.Add(message.game);
					break;
				case GameListStatusMsg.Operation.Remove:
					gamesList.Remove(gamesList.Find(game => game.uniqueId == message.game.uniqueId));
					break;
				case GameListStatusMsg.Operation.Update:
					var gameTemp = gamesList.Find(game => game.uniqueId == message.game.uniqueId);
					gameTemp.currentPlayers = message.game.currentPlayers;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			ReceiveMessage(message);
		}

		#endregion

		#region Sender

		private void RegisterPlayer() {
			Assert.IsTrue(_client.IsConnected);
			Debug.Log("[Client - GameManager] - Registering player"); 
			_client.NetworkSend(new RegisterPlayerMsg(), callbackMsg => {
				Debug.Log($"[Client - GameManager] - Received registration : {callbackMsg.status}");

				Assert.AreNotEqual(CallbackStatus.Default, callbackMsg.status);
				switch (callbackMsg.status) {
					case CallbackStatus.Success: {
						var responseReceived = (RegisterPlayerMsg) callbackMsg.message;

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
			Assert.IsTrue(_client.IsConnected);
			Debug.Log("[Client - GameManager] - Getting game list");
			
			_client.NetworkSend(new GameListMsg(), callbackMsg => {
				Debug.Log($"[Client - GameManager] - Received games list : {callbackMsg.status}");
				
				Assert.AreNotEqual(CallbackStatus.Default, callbackMsg.status);
				switch (callbackMsg.status) {
					case CallbackStatus.Success: {
						var responseReceived = (GameListMsg) callbackMsg.message;
						
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
				
				ReceiveResponse(callbackMsg.message, callbackMsg.status);
			});
		}

		public void CreateGame(CreateGameMsg createGameMsg) {
			Assert.IsFalse(IsInGame);
			Assert.IsTrue(_client.IsConnected);
			Debug.Log("[Client - GameManager] - Creating game ");
			createGameMsg.uniqueId = uniqueId;
			_client.NetworkSend(createGameMsg, HandleChangeServersMsg);
		}
		
		public void JoinGame(string gameUniqueId) {
			Assert.IsFalse(IsInGame);
			Assert.IsTrue(_client.IsConnected);
			Debug.Log("[Client - GameManager] - Joining game : " + gameUniqueId);

			_client.NetworkSend(new JoinGameMsg {
				uniqueId = uniqueId,
				gameUniqueId = gameUniqueId
			}, HandleChangeServersMsg);
		}

		public void LeaveGame() {
			Assert.IsTrue(IsInGame);
			Assert.IsTrue(_client.IsConnected);
			Debug.Log("[Client - GameManager] - Leaving game"); 
			
			_client.NetworkSend(new LeaveGameMsg{uniqueId = uniqueId});
			IsInGame = false;
			_netMananager.StopClient();
		}

		#endregion
	}
}