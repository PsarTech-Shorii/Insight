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

		public InsightMessage(InsightMessage insightMsg) {
			status = insightMsg.status;
			message = insightMsg.message;
		}

		public InsightMessage(InsightMessageBase msg) {
			message = msg;
		}

		public override void Deserialize(NetworkReader reader) {
			base.Deserialize(reader);
			callbackId = reader.ReadInt32();
			status = (CallbackStatus) reader.ReadByte();
			var msgType = Type.GetType(reader.ReadString());
			Assert.IsNotNull(msgType);
			message = (InsightMessageBase) Activator.CreateInstance(msgType);
			message.Deserialize(reader);
		}

		public override void Serialize(NetworkWriter writer) {
			base.Serialize(writer);
			writer.WriteInt32(callbackId);
			writer.WriteByte((byte) status);
			writer.WriteString(MsgType.FullName);
			message.Serialize(writer);
		}
	}

	public class InsightNetworkMessage : InsightMessage {
		public int connectionId;
		
		public InsightNetworkMessage() {}

		public InsightNetworkMessage(InsightMessageBase msg) : base(msg) {}
		public InsightNetworkMessage(InsightMessage insightMsg) : base(insightMsg) {}
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

		protected RequestSpawnStartMsg() {}

		protected RequestSpawnStartMsg(RequestSpawnStartMsg original) {
			gameUniqueId = original.gameUniqueId;
			networkAddress = original.networkAddress;
			networkPort = original.networkPort;
			gameName = original.gameName;
			minPlayers = original.minPlayers;
		}
	}

	public class RequestSpawnStartToMasterMsg : RequestSpawnStartMsg {
		public RequestSpawnStartToMasterMsg() {}
		public RequestSpawnStartToMasterMsg(RequestSpawnStartMsg original) : base(original) {}
	}

	public class RequestSpawnStartToSpawnerMsg : RequestSpawnStartMsg {
		public RequestSpawnStartToSpawnerMsg() {}
		public RequestSpawnStartToSpawnerMsg(RequestSpawnStartMsg original) : base(original) {}
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

		public void Load(List<GameContainer> gamesList) {
			gamesArray = gamesList.ToArray();
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