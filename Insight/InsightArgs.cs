using System;
using System.Linq;

namespace Insight {
	public static class ArgNames {
		public static string UniqueId => "-UniqueId";
		public static string NetworkAddress => "-NetworkAddress";
		public static string NetworkPort => "-NetworkPort";
		public static string GameName => "-GameName";
		public static string MinPlayers => "-MinPlayers";
	}
	
	public class InsightArgs {
		private readonly string[] _args;

		public InsightArgs() {
			_args = Environment.GetCommandLineArgs();
			
			UniqueId = ExtractValue(ArgNames.UniqueId);
			NetworkAddress = ExtractValue(ArgNames.NetworkAddress);
			NetworkPort = ExtractValueInt(ArgNames.NetworkPort);
			GameName = ExtractValue(ArgNames.GameName);
			MinPlayers = ExtractValueInt(ArgNames.MinPlayers);
		}

		#region Arguments
		
		public string UniqueId { get; }
		public string NetworkAddress { get; }
		public int NetworkPort { get; }
		public string GameName { get; }
		public int MinPlayers { get; }

		#endregion

		#region Helper methods

		private string ExtractValue(string argName, string defaultValue = null) {
			if (!_args.Contains(argName)) return defaultValue;

			var index = _args.ToList().FindIndex(0, a => a.Equals(argName));
			return _args[index + 1];
		}

		private int ExtractValueInt(string argName, int defaultValue = -1) {
			var number = ExtractValue(argName, defaultValue.ToString());
			return Convert.ToInt32(number);
		}

		public bool IsProvided(string argName) {
			return _args.Contains(argName);
		}

		#endregion
	}
}