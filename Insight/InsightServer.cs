using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class InsightServer : InsightCommon {
		private readonly List<int> _connectionsId = new List<int>();

		protected override void RegisterHandlers() {
			transport.OnServerConnected.AddListener(HandleConnect);
			transport.OnServerDisconnected.AddListener(HandleDisconnect);
			transport.OnServerDataReceived.AddListener(HandleData);
			transport.OnServerError.AddListener(OnError);
		}

		public override void StartInsight() {
			Debug.Log("[InsightServer] - Server started listening");

			transport.ServerStart();
			
			connectState = ConnectState.Connected;
		}

		public override void StopInsight() {
			Debug.Log("[InsightServer] - Server stopping");

			transport.ServerStop();

			connectState = ConnectState.Disconnected;
			
			_connectionsId.Clear();
		}

		private void HandleConnect(int connectionId) {
			Debug.Log($"[InsightServer] - Client connected connectionID: {connectionId}", this);
			
			_connectionsId.Add(connectionId);
		}

		private void HandleDisconnect(int connectionId) {
			Debug.Log($"[InsightServer] - Client disconnected connectionID: {connectionId}", this);

			_connectionsId.Remove(connectionId);
		}

		private void HandleData(int connectionId, ArraySegment<byte> data, int channelId) {
			if (!_connectionsId.Contains(connectionId)) {
				Debug.LogError($"HandleData: Unknown connectionId: {connectionId}", this);
				return;
			}
			
			var netMsg = new InsightNetworkMessage();
			netMsg.Deserialize(new NetworkReader(data));
			netMsg.connectionId = connectionId;

			HandleMessage(netMsg);
		}

		private void OnError(int connectionId, Exception exception) {
			// TODO Let's discuss how we will handle errors
			Debug.LogException(exception);
		}

		public void NetworkSend(int connectionId, InsightNetworkMessage netMsg, CallbackHandler callback = null) {
			if (!transport.ServerActive()) {
				Debug.LogError("Server.Send: not connected!", this);
				return;
			}

			netMsg.connectionId = connectionId;
			if(netMsg.callbackId == 0) RegisterCallback(netMsg, callback);
			
			var writer = new NetworkWriter();
			netMsg.Serialize(writer);

			transport.ServerSend(new List<int> {connectionId}, 0, writer.ToArraySegment());
		}

		public void NetworkSend(int connectionId, InsightMessageBase msg, CallbackHandler callback = null) {
			NetworkSend(connectionId, new InsightNetworkMessage(msg), callback);
		}

		public void NetworkReply(int connectionId, InsightNetworkMessage netMsg) {
			Assert.AreNotEqual(CallbackStatus.Default, netMsg.status);
			NetworkSend(connectionId, netMsg);
		}

		protected override void Resend(InsightMessage insightMsg, CallbackHandler callback) {
			if (insightMsg is InsightNetworkMessage netMsg) {
				NetworkSend(netMsg.connectionId, netMsg, callback);
			}
			else {
				InternalSend(insightMsg, callback);
			}
		}
	}
}