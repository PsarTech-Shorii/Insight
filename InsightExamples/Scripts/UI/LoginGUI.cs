using Insight;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace UI {
	public class LoginGUI : MonoBehaviour {
		[Header("Module")]
		[SerializeField] private ClientAuthentication clientAuthentication;

		[Header("UI")]
		[SerializeField] private TextMeshProUGUI usernameText;
		[SerializeField] private TextMeshProUGUI statusText;

		public UnityEvent toActivate;

		private void Start() {
			clientAuthentication.onResponse.AddListener(OnLogin);
		}

		public void SendLoginMsg() {
			clientAuthentication.SendLoginMsg(new LoginMsg{accountName = usernameText.text});
		}
		
		private void OnLogin(InsightMessageBase messageBase, CallbackStatus status) {
			if(!(messageBase is LoginMsg)) return;
			
			if (status == CallbackStatus.Success) {
				toActivate?.Invoke();
				gameObject.SetActive(false);
			}
			else {
				statusText.text = $"Login : {status}";
			}
		}
	}
}