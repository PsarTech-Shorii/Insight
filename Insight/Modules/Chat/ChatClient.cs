using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class ChatClient : InsightModule {
		private InsightClient _client;

		public override void Initialize(InsightClient client, ModuleManager manager) {
			_client = client;

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			_client.RegisterHandler<ChatMsg>(HandleChatMsg);
		}

		private void HandleChatMsg(InsightMessage insightMsg) {
			Debug.Log("[Client - Chat] - Receive chatting");

			var message = (ChatMsg) insightMsg.message;
			
			ReceiveMessage(message);
		}

		public void Chat(string data) {
			Assert.IsTrue(_client.IsConnected);
			_client.NetworkSend(new ChatMsg {data = data});
		}
	}
}