using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class ClientAuthentication : InsightModule {
		private InsightClient _client;
		private string _uniqueId;

		public override void Initialize(InsightClient client, ModuleManager manager) {
			_client = client;

			Debug.Log("[Client - Authentication] - Initialization");

			RegisterHandlers();
		}

		private void RegisterHandlers() {}

		public void SendLoginMsg(LoginMsg message) {
			Assert.IsTrue(_client.IsConnected);
			Debug.Log("[Client - Authentication] - Logging in");

			_client.NetworkSend(message, callbackMsg => {
				Debug.Log($"[Client - Authentication] - Received login response : {callbackMsg.status}");

				Assert.AreNotEqual(CallbackStatus.Default, callbackMsg.status);
				switch (callbackMsg.status) {
					case CallbackStatus.Success: {
						var responseReceived = (LoginMsg) callbackMsg.message;

						_uniqueId = responseReceived.uniqueId;

						break;
					}
					case CallbackStatus.Error:
					case CallbackStatus.Timeout:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				
				onResponse?.Invoke(callbackMsg.message, callbackMsg.status);
			});
		}
	}
}