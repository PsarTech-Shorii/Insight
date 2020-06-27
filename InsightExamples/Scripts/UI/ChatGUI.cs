using Insight;
using TMPro;
using UnityEngine;

namespace UI {
	public class ChatGUI : MonoBehaviour {
		[Header("Module")] 
		[SerializeField] private ChatClient chatClient;
		
		[Header("Interface")]
		[SerializeField] private TextMeshProUGUI chatLogText;
		[SerializeField] private TextMeshProUGUI chatText;

		private void Start() {
			chatClient.onReceive.AddListener(OnReceiveChat);
			gameObject.SetActive(false);
		}

		private void OnEnable() {
			chatLogText.text = "";
			chatText.text = "";
		}

		public void Chat() {
			chatClient.Chat(chatText.text);
		}

		private void OnReceiveChat(InsightMessageBase messageBase) {
			if(!(messageBase is ChatMsg message)) return;
			chatLogText.text += $"{message.username} : {message.data}\n";
		}
	}
}