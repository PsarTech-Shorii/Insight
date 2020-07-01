using Mirror;
using UnityEngine;

namespace UI {
	public class ConnectingGUI : MonoBehaviour {
		[SerializeField] private Transport transport;
		[SerializeField] private CanvasGroup canvasGroup;

		private void Start() {
			transport.OnClientConnected.AddListener(OnConnected);
			transport.OnClientDisconnected.AddListener(OnDisconnected);
		}

		private void OnConnected() {
			gameObject.SetActive(false);
			canvasGroup.interactable = true;
		}
		
		private void OnDisconnected() {
			gameObject.SetActive(true);
			canvasGroup.interactable = false;
		}
	}
}