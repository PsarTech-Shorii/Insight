using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class ClientAuthentication : InsightModule {
		public delegate void Login(bool _newValue);

		private InsightClient client;
		
		private string uniqueId;
		private bool isLogin;
		
		public event Login OnLogin;
		public bool IsLogin {
			get => isLogin;
			private set {
				isLogin = value;
				OnLogin?.Invoke(isLogin);
			}
		}

		public override void Initialize(InsightClient _client, ModuleManager _manager) {
			Debug.Log("[ClientAuthentication] - Initialization");
			client = _client;

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			client.OnDisconnected += HandleDisconnect;
		}

		private void HandleDisconnect() {
			uniqueId = null;
			IsLogin = false;
		}
		
		public void SendLoginMsg(LoginMsg _message) {
			Assert.IsFalse(IsLogin);
			Assert.IsTrue(client.IsConnected);
			Debug.Log("[ClientAuthentication] - Logging in");

			client.NetworkSend(_message, _callbackMsg => {
				Debug.Log($"[ClientAuthentication] - Received login response : {_callbackMsg.status}");

				Assert.AreNotEqual(CallbackStatus.Default, _callbackMsg.status);
				switch (_callbackMsg.status) {
					case CallbackStatus.Success: {
						var responseReceived = (LoginMsg) _callbackMsg.message;

						uniqueId = responseReceived.uniqueId;
						IsLogin = true;

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
	}
}