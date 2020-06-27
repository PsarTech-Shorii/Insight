using Mirror;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class InsightClient : InsightCommon {
		private float _reconnectTimer;
		private int _serverConnId;

		public bool autoReconnect = true;
		public float reconnectDelayInSeconds = 5f;

		protected override void Update() {
			base.Update();
			CheckConnection();
		}

		protected override void RegisterHandlers() {
			transport.OnClientConnected.AddListener(OnConnected);
			transport.OnClientDisconnected.AddListener(OnDisconnected);
			transport.OnClientDataReceived.AddListener(HandleData);
			transport.OnClientError.AddListener(OnError);
		}

		public override void StartInsight() {
			transport.ClientConnect(networkAddress);

			_reconnectTimer = Time.realtimeSinceStartup + reconnectDelayInSeconds;
		}

		public override void StopInsight() {
			transport.ClientDisconnect();
		}

		private void OnConnected() {
			Debug.Log($"[InsightClient] - Connecting to Insight Server: {networkAddress}");
			connectState = ConnectState.Connected;
		}

		private void OnDisconnected() {
			Debug.Log("[InsightClient] - Disconnecting from Insight Server");
			connectState = ConnectState.Disconnected;
		}

		private void HandleData(ArraySegment<byte> data, int channelId) {
			var netMsg = new InsightNetworkMessage();
			netMsg.Deserialize(new NetworkReader(data));

			HandleMessage(netMsg);
		}

		private void OnError(Exception exception) {
			// TODO Let's discuss how we will handle errors
			Debug.LogException(exception);
		}

		private void CheckConnection() {
			if (autoReconnect) {
				if (!IsConnected && (_reconnectTimer < Time.time)) {
					Debug.Log("[InsightClient] - Trying to reconnect...");

					_reconnectTimer = Time.realtimeSinceStartup + reconnectDelayInSeconds;
					StartInsight();
				}
			}
		}

		public void NetworkSend(InsightNetworkMessage netMsg, CallbackHandler callback = null) {
			if (!transport.ClientConnected()) {
				Debug.LogError("[InsightClient] - Client not connected!");
				return;
			}

			if(netMsg.callbackId == 0) RegisterCallback(netMsg, callback);
			
			var writer = new NetworkWriter();
			netMsg.Serialize(writer);

			transport.ClientSend(0, writer.ToArraySegment());
		}

		public void NetworkSend(InsightMessageBase msg, CallbackHandler callback = null) {
			NetworkSend(new InsightNetworkMessage(msg), callback);
		}

		public void NetworkReply(InsightNetworkMessage netMsg) {
			Assert.AreNotEqual(CallbackStatus.Default, netMsg.status);
			NetworkSend(netMsg);
		}
		
		protected override void Resend(InsightMessage insightMsg, CallbackHandler callback) {
			if (insightMsg is InsightNetworkMessage netMsg) {
				NetworkSend(netMsg, callback);
			}
			else {
				InternalSend(insightMsg, callback);
			}
		}
	}
}