using Insight;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace UI {
	public class JoinEvent : UnityEvent<string> {}
	
	public class GameJoiner : MonoBehaviour {
		[SerializeField] private TextMeshProUGUI gameNameText;
		[SerializeField] private TextMeshProUGUI countText;
		
		private GameContainer _game;

		public JoinEvent joinEvent = new JoinEvent();

		public void Initialize(GameContainer game) {
			_game = game;
			gameNameText.text = _game.gameName;
			countText.text = $"{_game.currentPlayers} / {_game.minPlayers}";
			
		}

		public void UpdatePlayerCount(GameStatusMsg gameStatus) {
			_game.currentPlayers = gameStatus.currentPlayers;
			_game.hasStarted = gameStatus.hasStarted;
			countText.text = $"{_game.currentPlayers} / {_game.minPlayers}";
			gameObject.SetActive(!_game.hasStarted && _game.currentPlayers < _game.maxPlayers);
		}

		public bool Is(string uniqueId) {
			return _game.uniqueId == uniqueId;
		}

		public void JoinGame() {
			joinEvent?.Invoke(_game.uniqueId);
		}
	}
}