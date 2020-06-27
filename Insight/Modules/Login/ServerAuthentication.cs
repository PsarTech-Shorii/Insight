using System;
using System.Collections.Generic;
using UnityEngine;

namespace Insight {
	public class ServerAuthentication : InsightModule {
		private InsightServer _server;

		[HideInInspector] public List<UserContainer> registeredUsers = new List<UserContainer>();

		public override void Initialize(InsightServer server, ModuleManager manager) {
			_server = server;
			
			Debug.Log("[Server - Authentication] - Initialization");

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			_server.transport.OnServerDisconnected.AddListener(HandleDisconnect);
			
			_server.RegisterHandler<LoginMsg>(HandleLoginMsg);
		}
		
		private void HandleDisconnect(int connectionId) {
			registeredUsers.Remove(registeredUsers.Find(e => e.connectionId == connectionId));
		}
		
		private void HandleLoginMsg(InsightMessage insightMsg) {
			if (insightMsg is InsightNetworkMessage netMsg) {
				var message = (LoginMsg) insightMsg.message;
				
				Debug.Log($"[Server - Authentication] - Received login : {message.accountName} / {message.accountPassword}");

				if (Authenticated(message)) { //Login Sucessful
					var uniqueId = Guid.NewGuid().ToString();

					registeredUsers.Add(new UserContainer {
						connectionId = netMsg.connectionId,
						uniqueId = uniqueId,
						username = message.accountName
					});

					var responseToSend = new InsightNetworkMessage(new LoginMsg{uniqueId = uniqueId}) {
						callbackId = insightMsg.callbackId,
						status = CallbackStatus.Success
					};
					_server.NetworkReply(netMsg.connectionId, responseToSend);
				}
				else { //Login Failed
					var responseToSend = new InsightNetworkMessage(new LoginMsg()) {
						callbackId = insightMsg.callbackId,
						status = CallbackStatus.Error
					};
					_server.NetworkReply(netMsg.connectionId, responseToSend);
				}
			}
			else {
				Debug.LogError("[Server - Authentication] - Rejected (internal) login");
			}
		}

		private bool Authenticated(LoginMsg message) { //Put your DB logic here
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