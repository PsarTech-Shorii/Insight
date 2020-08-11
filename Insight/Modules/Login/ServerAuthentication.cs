using System;
using System.Collections.Generic;
using UnityEngine;

namespace Insight {
	public class ServerAuthentication : InsightModule {
		private InsightServer server;
		
		[HideInInspector] public List<UserContainer> registeredUsers = new List<UserContainer>();

		public override void Initialize(InsightServer _server, ModuleManager _manager) {
			Debug.Log("[ServerAuthentication] - Initialization");
			server = _server;

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			server.transport.OnServerDisconnected.AddListener(HandleDisconnect);
			
			server.RegisterHandler<LoginMsg>(HandleLoginMsg);
		}
		
		private void HandleDisconnect(int _connectionId) {
			registeredUsers.Remove(registeredUsers.Find(_e => _e.connectionId == _connectionId));
		}
		
		private void HandleLoginMsg(InsightMessage _insightMsg) {
			if (_insightMsg is InsightNetworkMessage netMsg) {
				var message = (LoginMsg) _insightMsg.message;
				
				Debug.Log($"[ServerAuthentication] - Received login : {message.accountName} / {message.accountPassword}");

				if (Authenticated(message)) { //Login Sucessful
					var uniqueId = Guid.NewGuid().ToString();

					registeredUsers.Add(new UserContainer {
						connectionId = netMsg.connectionId,
						uniqueId = uniqueId,
						username = message.accountName
					});

					var responseToSend = new InsightNetworkMessage(new LoginMsg{uniqueId = uniqueId}) {
						callbackId = _insightMsg.callbackId,
						status = CallbackStatus.Success
					};
					server.NetworkReply(netMsg.connectionId, responseToSend);
				}
				else { //Login Failed
					var responseToSend = new InsightNetworkMessage(new LoginMsg()) {
						callbackId = _insightMsg.callbackId,
						status = CallbackStatus.Error
					};
					server.NetworkReply(netMsg.connectionId, responseToSend);
				}
			}
			else {
				Debug.LogError("[ServerAuthentication] - Rejected (internal) login");
			}
		}

		private bool Authenticated(LoginMsg _message) { //Put your DB logic here
			return true;
		}
	}

	[Serializable]
	public class UserContainer {
		public int connectionId;
		public string uniqueId;
		public string username;
	}
}