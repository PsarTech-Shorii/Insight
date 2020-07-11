using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class ClientAuthentication : InsightModule {
		public delegate void Login(bool newValue);
		
		private InsightClient _client;
		private string _uniqueId;
		
		private bool _isLogin;
		
		public event Login OnLogin;
		public bool IsLogin {
			get => _isLogin;
			private set {
				_isLogin = value;
				OnLogin?.Invoke(_isLogin);
			}
		}

		public override void Initialize(InsightClient client, ModuleManager manager) {
			_client = client;

			Debug.Log("[Client - Authentication] - Initialization");

			RegisterHandlers();
		}

		private void RegisterHandlers() {}

		public void SendLoginMsg(LoginMsg message) {
			Assert.IsFalse(IsLogin);
			Assert.IsTrue(_client.IsConnected);
			Debug.Log("[Client - Authentication] - Logging in");

			_client.NetworkSend(message, callbackMsg => {
				Debug.Log($"[Client - Authentication] - Received login response : {callbackMsg.status}");

				Assert.AreNotEqual(CallbackStatus.Default, callbackMsg.status);
				switch (callbackMsg.status) {
					case CallbackStatus.Success: {
						var responseReceived = (LoginMsg) callbackMsg.message;

						_uniqueId = responseReceived.uniqueId;
						IsLogin = true;

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
	}
}