using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ClientWPF
{
	/*
	 1) Реализовать клиент-серверное приложение, позволяющее пользователям играть в игру.
	 Варианты игр: лото, карточная игра, шашки, шахматы, крестики-нолики,
    "Быки и коровы". Клиент - Windows Forms или WPF приложение, сервер - консольное приложение.
	 */
	public partial class MainWindow : Window
	{
		PlayWindow playWindow;//игровое окно

		TcpClient clientSocket;//сокет игрока

		Guid id;//ID игрока		

		public MainWindow()
		{
			InitializeComponent();
			id = Guid.NewGuid();
			idElement.Text = id.ToString();
			clientSocket = new TcpClient();
		}
		private void StartGameButtonClick(object sender, RoutedEventArgs e)
		{
			if (!clientSocket.Connected)
			{
				MessageBox.Show("Не подключен сервер!");
			}
			else
			{
				playWindow = new PlayWindow(clientSocket, id);
				playWindow.Show();
				this.Close();
			}
		}
		private void ConnectButtonClick(object sender, RoutedEventArgs e)
		{
			try
			{
				int port = 12345;
				string ipServer = "127.0.0.1";
				clientSocket.Connect(ipServer, port);
				if (clientSocket.Client.Connected)
				{
					MessageBox.Show("Сервер подключен");
					
				}
				else { MessageBox.Show("нет сигнала"); }
			}
			catch (Exception exception)
			{
				MessageBox.Show($"Error: {exception}");
			}			
		}
	}
}
