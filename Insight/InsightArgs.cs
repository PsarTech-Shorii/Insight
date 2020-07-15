using System;
using System.Linq;

namespace Insight {
	public static class ArgNames {
		public static string UniqueId => "-UniqueId";
		public static string NetworkAddress => "-NetworkAddress";
		public static string NetworkPort => "-NetworkPort";
		public static string GameName => "-GameName";
		public static string MinPlayers => "-MinPlayers";
		public static string MaxPlayers => "-MaxPlayers";
	}
	
	public class InsightArgs {
		private readonly string[] args;

		public InsightArgs() {
			args = Environment.GetCommandLineArgs();
			
			UniqueId = ExtractValue(ArgNames.UniqueId);
			NetworkAddress = ExtractValue(ArgNames.NetworkAddress);
			NetworkPort = ExtractValueInt(ArgNames.NetworkPort);
			GameName = ExtractValue(ArgNames.GameName);
			MinPlayers = ExtractValueInt(ArgNames.MinPlayers);
			MaxPlayers = ExtractValueInt(ArgNames.MaxPlayers);
		}

		#region Arguments
		
		public string UniqueId { get; }
		public string NetworkAddress { get; }
		public int NetworkPort { get; }
		public string GameName { get; }
		public int MinPlayers { get; }
		public int MaxPlayers { get; }

		#endregion

		#region Helper methods

		private string ExtractValue(string _argName, string _defaultValue = null) {
			if (!args.Contains(_argName)) return _defaultValue;

			var index = args.ToList().FindIndex(0, _a => _a.Equals(_argName));
			return args[index + 1];
		}

		private int ExtractValueInt(string _argName, int _defaultValue = -1) {
			var number = ExtractValue(_argName, _defaultValue.ToString());
			return Convert.ToInt32(number);
		}

		public bool IsProvided(string _argName) {
			return args.Contains(_argName);
		}

		#endregion
	}
}