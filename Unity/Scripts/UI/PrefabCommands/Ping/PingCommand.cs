#nullable enable
using System.Collections;
using System.Threading.Tasks;
using Architecture;
using OS.Network;

namespace UI.PrefabCommands.Ping
{
	public class PingCommand : CommandScript
	{
		/// <inheritdoc />
		protected override async Task OnMain()
		{
			if (Network == null)
			{
				Console.WriteLine("No network connection");
				EndProcess();
				return;
			}

			if (Arguments.Length != 1)
			{
				Console.WriteLine("ping: Usage: ping <host>");
				EndProcess();
				return;
			}

			string host = Arguments[0];
			if (!Network.Resolve(host, out uint address))
			{
				Console.WriteLine($"ping: could not resolve host {host}");
				EndProcess();
				return;
			}

			await Ping(address, 32);
			EndProcess();
		}

		private async Task Ping(uint address, int amountToPing)
		{
			for (var i = 0; i < amountToPing; i++)
			{
				if (Network == null)
				{
					Console.WriteLine("No network connection");
					EndProcess();
					return;
				}

				PingResult result = await Network.Ping(address, 4, false);

				switch (result)
				{
					case PingResult.TimedOut:
						Console.WriteLine("Request timed out");
						break;
					case PingResult.Pong:
						Console.WriteLine($"Reply from {NetUtility.GetNetworkAddressString(address)}");
						await Task.Delay(100);
						break;
				}
			}
		}
	}
}