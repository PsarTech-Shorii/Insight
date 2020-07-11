using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class InsightClient : InsightCommon {
		private int _serverConnId;

		private IEnumerator _reconnectCor;

		public bool autoReconnect = true;
		public float reconnectDelayInSeconds = 5f;

		protected override void RegisterHandlers() {
			transport.OnClientConnected.AddListener(OnConnected);
			transport.OnClientDisconnected.AddListener(OnDisconnected);
			transport.OnClientDataReceived.AddListener(HandleData);
			transport.OnClientError.AddListener(OnError);
		}

		public override void StartInsight() {
			transport.ClientConnect(networkAddress);

			if(_reconnectCor != null) {
				StopCoroutine(_reconnectCor);
				_reconnectCor = null;
			}
			_reconnectCor = ReconnectCor();
			StartCoroutine(_reconnectCor);
		}

		public override void StopInsight() {
			transport.ClientDisconnect();
		}

		private void OnConnected() {
			Debug.Log($"[InsightClient] - Connecting to Insight Server: {networkAddress}");
			
			if(_reconnectCor != null) {
				StopCoroutine(_reconnectCor);
				_reconnectCor = null;
			}
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

		private IEnumerator ReconnectCor() {
			Assert.IsTrue(autoReconnect);
			Assert.IsFalse(IsConnected);
				
			yield return new WaitForSeconds(reconnectDelayInSeconds);
				
			Debug.Log("[InsightClient] - Trying to reconnect...");
			StartInsight();
		}
	}
}