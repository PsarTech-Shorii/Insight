using System;
using Mirror;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Insight {
	public class InsightMessageBase : MessageBase {}
	
	public class InsightMessage : InsightMessageBase {
		public int callbackId;
		public CallbackStatus status = CallbackStatus.Default;
		public InsightMessageBase message;

		public Type MsgType => message.GetType();

		public InsightMessage() {
			message = new EmptyMessage();
		}

		public InsightMessage(InsightMessage _insightMsg) {
			status = _insightMsg.status;
			message = _insightMsg.message;
		}

		public InsightMessage(InsightMessageBase _message) {
			message = _message;
		}

		public override void Deserialize(NetworkReader _reader) {
			base.Deserialize(_reader);
			callbackId = _reader.ReadInt32();
			status = (CallbackStatus) _reader.ReadByte();
			var msgType = Type.GetType(_reader.ReadString());
			Assert.IsNotNull(msgType);
			message = (InsightMessageBase) Activator.CreateInstance(msgType);
			message.Deserialize(_reader);
		}

		public override void Serialize(NetworkWriter _writer) {
			base.Serialize(_writer);
			_writer.WriteInt32(callbackId);
			_writer.WriteByte((byte) status);
			_writer.WriteString(MsgType.FullName);
			message.Serialize(_writer);
		}
	}

	public class InsightNetworkMessage : InsightMessage {
		public int connectionId;
		
		public InsightNetworkMessage() {}

		public InsightNetworkMessage(InsightMessageBase _message) : base(_message) {}
		public InsightNetworkMessage(InsightMessage _insightMsg) : base(_insightMsg) {}
	}
	
	public class EmptyMessage : InsightMessageBase {}
	
	#region Spawner

	public class RegisterSpawnerMsg : InsightMessageBase {
		public string uniqueId; //Guid
		public int maxThreads;
	}
	
	public class SpawnerStatusMsg : InsightMessageBase {
		public string uniqueId; //Guid
		public int currentThreads;
	}
	
	public abstract class RequestSpawnStartMsg : InsightMessageBase {
		public string gameUniqueId; //Guid
		public string networkAddress;
		public ushort networkPort;
		public string gameName;
		public int minPlayers;
		public int maxPlayers;

		protected RequestSpawnStartMsg() {}

		protected RequestSpawnStartMsg(RequestSpawnStartMsg _original) {
			gameUniqueId = _original.gameUniqueId;
			networkAddress = _original.networkAddress;
			networkPort = _original.networkPort;
			gameName = _original.gameName;
			minPlayers = _original.minPlayers;
			maxPlayers = _original.maxPlayers;
		}
	}

	public class RequestSpawnStartToMasterMsg : RequestSpawnStartMsg {
		public RequestSpawnStartToMasterMsg() {}
		public RequestSpawnStartToMasterMsg(RequestSpawnStartMsg _original) : base(_original) {}
	}

	public class RequestSpawnStartToSpawnerMsg : RequestSpawnStartMsg {
		public RequestSpawnStartToSpawnerMsg() {}
		public RequestSpawnStartToSpawnerMsg(RequestSpawnStartMsg _original) : base(_original) {}
	}

	public class KillSpawnMsg : InsightMessageBase {
		public string uniqueId; //Guid
	}

	#endregion

	#region GameManager

	public class RegisterGameMsg : InsightMessageBase {
		public string uniqueId; //Guid
		public string networkAddress;
		public ushort networkPort;
		public string gameName;
		public int minPlayers;
		public int maxPlayers;
		public int currentPlayers;
	}

	public class RegisterPlayerMsg : InsightMessageBase {
		public string uniqueId; //Guid
	}

	public class ChangeServerMsg : InsightMessageBase {
		public string uniqueId;
		public string networkAddress;
		public ushort networkPort;
	}

	public class CreateGameMsg : InsightMessageBase {
		public string uniqueId; //Guid
		public string gameName;
		public int minPlayers;
		public int maxPlayers;
	}
	
	public class JoinGameMsg : InsightMessageBase {
		public string uniqueId; //Guid
		public string gameUniqueId;
	}

	public class LeaveGameMsg : InsightMessageBase {
		public string uniqueId; //Guid
	}

	public class GameStatusMsg : InsightMessageBase {
		public string uniqueId; //Guid
		public int currentPlayers;
		public bool hasStarted;
	}
	
	public class GameListMsg : InsightMessageBase {
		public GameContainer[] gamesArray;

		public void Load(List<GameContainer> _gamesList) {
			gamesArray = _gamesList.ToArray();
		}
	}

	public class GameListStatusMsg : InsightMessageBase {
		public enum Operation {
			Add,
			Remove,
			Update
		}
		
		public Operation operation;
		public GameContainer game;
	}

	#endregion

	#region Login
	
	public class LoginMsg : InsightMessageBase {
		public string uniqueId; //Guid
		public string accountName;
		public string accountPassword;
	}

	#endregion

	#region Chat

	public class ChatMsg : InsightMessageBase {
		public string username;
		public string data;
	}

	#endregion

	#region MatchMaking

	public class MatchGameMsg : InsightMessageBase {
		public string uniqueId; //Guid
	}

	#endregion
}