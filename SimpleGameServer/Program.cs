using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace SimpleGameServer
{
	/*
	 1) Реализовать клиент-серверное приложение, позволяющее пользователям играть в игру.
	 Варианты игр: лото, карточная игра, шашки, шахматы, крестики-нолики,
    "Быки и коровы". Клиент - Windows Forms или WPF приложение, сервер - консольное приложение.
	 */
	class Program
	{
		static void Main(string[] args)
		{
			ClientGroup.Group = new List<ClientInfo>();//создаем списки игроков
			Console.WriteLine("Максимальная вместимость сервера 2 игрока\n");
			ClientGroup.Limit = 0;
			//Создадим сервер
			TcpListener socketServer;
			int port = 12345;
			string ipAddress = "0.0.0.0";
			socketServer = new TcpListener(IPAddress.Parse(ipAddress), port);
			socketServer.Start(100);			

			while (true)
			{
				if (ClientGroup.Limit < 2)
				{					
					//ждем клиента
					IAsyncResult iAsyncResult = socketServer.BeginAcceptTcpClient(AcceptClientProc, socketServer);
					//ожидание завершения асинхронного соединения со стороны клиента
					iAsyncResult.AsyncWaitHandle.WaitOne();					
				}
				else
				{				
					socketServer.Stop();
				}
			}
		}
		static void AcceptClientProc(IAsyncResult iARes)
		{
			ClientGroup.Limit++;
			TcpListener socketServer = (TcpListener)iARes.AsyncState;
			TcpClient client = socketServer.EndAcceptTcpClient(iARes);//(мы дождались клиента)сокет для обмена данными
			Console.WriteLine($"Клиент прибыл: {client.Client.RemoteEndPoint.ToString()}");
			ThreadPool.QueueUserWorkItem(ClientThreadProc, client);
		}
		static void ClientThreadProc(object obj)
		{
			TcpClient client = (TcpClient)obj;
			byte[] recBuf = new byte[4 * 1024];
			
			int recSize = client.Client.Receive(recBuf);
			Guid id = Guid.Parse(Encoding.UTF8.GetString(recBuf, 0, recSize));
			Console.WriteLine($"ID : {id}");
			client.Client.Send(Encoding.UTF8.GetBytes($"Привет {ClientGroup.Limit.ToString()}-ый игрок ID: {id}"));

			Console.WriteLine($"{ClientGroup.Limit}-ый игрок");

			//список клиентов
			ClientInfo oneClient = new ClientInfo { ID=id, ClientSocket =client};
			ClientGroup.Group.Add(oneClient);

			if (ClientGroup.Limit == 2)
			{
				Console.WriteLine($"Достигнуто макс. кол-во игроков ({ClientGroup.Limit})");
			}
			try
			{
				while (true)
				{
					recSize = client.Client.Receive(recBuf);
					string sender = Encoding.UTF8.GetString(recBuf, 0, recSize);
					Console.WriteLine($"\nПринят: {sender} от игрока: {id}");

					if (recSize <= 0)
					{
						break;//связь разорвана клиентом
					}
					//ответ серверу и всем клиентам сообщение от одного клиента
					foreach (var a in ClientGroup.Group)
					{
						//проверка клиента на связь
						if ((a.ClientSocket.Client.Connected) &&(a.ID!=id))
						{
							Console.WriteLine($"Игроку {a.ID} отправлено {sender}");
							a.ClientSocket.Client.Send(Encoding.UTF8.GetBytes(sender));
							//a.ClientSocket.Client.Send(recBuf, recSize, SocketFlags.None);
						}
					}					
				}
				foreach (var a in ClientGroup.Group)
				{
					if (a.ClientSocket == client)
					{
						ClientGroup.Group.Remove(a);
						break;
					}
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine("\nError: "+exception);
			}
			client.Client.Shutdown(SocketShutdown.Both);
			client.Close();
		}
	}
}
