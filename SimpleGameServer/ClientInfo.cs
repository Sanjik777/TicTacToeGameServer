using System;
using System.Net.Sockets;

namespace SimpleGameServer
{
	public class ClientInfo
	{
		public Guid ID { get; set; } = Guid.NewGuid();
		public TcpClient ClientSocket { get; set; } = new TcpClient();
	}
}
