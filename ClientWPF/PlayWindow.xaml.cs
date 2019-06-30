using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ClientWPF
{
	public partial class PlayWindow : Window
	{
		Thread threadSendReceiveData;//поток обмена игрока
		TcpClient clientSocket;

		Symbol[] symbols;
		bool playerTurnSymbol; //чередование символов для игрокков
		bool gameOver;

		byte[] recBuf;
		int recSize;

		int index;//позиция в координате
		string sendInfo;//отправить сообщение
		string comeInfo;//принять сообщение

		bool playerRound;//очередь игрока
		string symb;//символ кнопки через диспатчер
		int coordinate;//координата через диспатчер

		public PlayWindow(TcpClient client, Guid id)
		{
			InitializeComponent();
			clientSocket = client;

			recBuf = new byte[4 * 1024];
			client.Client.Send(Encoding.UTF8.GetBytes(id.ToString()));//отправить серверу свой id
			recSize = client.Client.Receive(recBuf);
			string fisrtConnectString = Encoding.UTF8.GetString(recBuf, 0, recSize);
			MessageBox.Show(fisrtConnectString);//приветствие от сервера

			if (fisrtConnectString.Contains("1-ый"))//первое действие для первого игрока
			{
				playerRound = true;
				playerTurnSymbol = true;
			}
			else if (fisrtConnectString.Contains("2-ый"))//первое действие для второго игрока
			{
				playerRound = false;
				playerTurnSymbol = false;

				DisableButton();//вырубим кнопки второго до дейсвтвия первого игрока
				if (!clientSocket.Client.Connected)
				{
					MessageBox.Show("Вы не подключились к серверу!");
				}
				else
				{
					//ожидаем в трэде действие первого игрока (этот трэд вызовем только один раз в начале игры)
					threadSendReceiveData = new Thread(new ThreadStart(WaitingSecondPlayerThread));
					threadSendReceiveData.IsBackground = true;
					threadSendReceiveData.Start();
				}
			}
			NewGame();
		}
		void NewGame()
		{
			symbols = new Symbol[9];
			for (int i = 0; i < symbols.Length; i++)
			{
				symbols[i] = Symbol.Free;
			}
			//playerTurnSymbol = true;
			gameGrid.Children.Cast<Button>().ToList().ForEach(button =>
			{
				button.Content = string.Empty;
			});
			gameOver = false;//Игра еще не окончена
		}
		void NewGameInvoke()
		{
			Dispatcher.Invoke(new Action(
				() =>
				{
					NewGame();
				}
				));
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (gameOver)
			{
				NewGame();
				return;
			}
			//определение кнопок координат
			var button = (Button)sender;
			var column = Grid.GetColumn(button);
			var row = Grid.GetRow(button);
			index = column + (row * 3);

			if (symbols[index] != Symbol.Free)
			{
				return;//нельзя трогать занятые ячейки
			}

			symbols[index] = playerTurnSymbol ? Symbol.Cross : Symbol.Nought;
			button.Content = playerTurnSymbol ? "X" : "O";
			//playerTurnSymbol ^= true;

			sendInfo = button.Content.ToString();
			//CheckVictory();
			StartThread();
		}
		private void StartThread()
		{
			if (!clientSocket.Client.Connected)
			{
				MessageBox.Show("Вы не подключились к серверу!");
			}
			else
			{
				threadSendReceiveData = new Thread(new ThreadStart(StartSendReceiving));
				threadSendReceiveData.IsBackground = true;				
				threadSendReceiveData.Start();
			}
		}

		private void StartSendReceiving()
		{
			try
			{
				//Действие для первого игрока
				if (playerRound)
				{
					CheckVictory();
					if (gameOver)
					{
						NewGameInvoke();
					}
					clientSocket.Client.Send(Encoding.UTF8.GetBytes($"символ: {sendInfo} координата: {index}"));

					DisableButton();
					recSize = clientSocket.Client.Receive(recBuf);
					comeInfo = Encoding.UTF8.GetString(recBuf, 0, recSize);
					//извлекаем координату кнопки из пришедшего сообщения
					coordinate = int.Parse(Regex.Match(comeInfo, @"\d+").Value);

					if (comeInfo.Contains("X"))
					{
						symb = "X";
						//MessageBox.Show($"Prinjat symbol {symb} i coordi {coordinate}");
						ChangeButton(coordinate,symb);
						EnableButton();
					}
					else if (comeInfo.Contains("O"))
					{
						symb = "O";
						ChangeButton(coordinate,symb);
						EnableButton();
					}
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show("Error: " + exception);
			}
		}
		//ожидаем в трэде действие первого игрока (этот трэд вызовем только один раз в начале игры)
		private void WaitingSecondPlayerThread()
		{
			try
			{
				recSize = clientSocket.Client.Receive(recBuf);
				comeInfo = Encoding.UTF8.GetString(recBuf, 0, recSize);
				//извлекаем координату кнопки из пришедшего сообщения
				coordinate = int.Parse(Regex.Match(comeInfo, @"\d+").Value);
				if (comeInfo.Contains("X"))
				{
					symb = "X";
					ChangeButton(coordinate,symb);
					EnableButton();
				}
				else if (comeInfo.Contains("O"))
				{
					symb = "O";
					ChangeButton(coordinate,symb);
					EnableButton();
				}
				playerRound = true;
			}
			catch (Exception exception)
			{
				MessageBox.Show("Error: " + exception);
			}
		}

		#region Диспатчеры для кнопок
		void ChangeButton(int coordinate, string symb)
		{
			Dispatcher.Invoke(new Action(
				() =>
				{
					
					bool anotherTurn = false;
					if (symb=="X")
					{
						anotherTurn = true;
					}
					else if (symb=="O")
					{
						anotherTurn = false;
					}

					if (coordinate == 0)
					{
						button0.Content = symb;
						symbols[coordinate] = anotherTurn ? Symbol.Cross : Symbol.Nought;
					}
					else if (coordinate == 1)
					{
						button1.Content = symb;
						symbols[coordinate] = anotherTurn ? Symbol.Cross : Symbol.Nought;
					}
					else if (coordinate == 2)
					{
						button2.Content = symb;
						symbols[coordinate] = anotherTurn ? Symbol.Cross : Symbol.Nought;
					}
					else if (coordinate == 3)
					{
						button3.Content = symb;
						symbols[coordinate] = anotherTurn ? Symbol.Cross : Symbol.Nought;
					}
					else if (coordinate == 4)
					{
						button4.Content = symb;
						symbols[coordinate] = anotherTurn ? Symbol.Cross : Symbol.Nought;
					}
					else if (coordinate == 5)
					{
						button5.Content = symb;
						symbols[coordinate] = anotherTurn ? Symbol.Cross : Symbol.Nought;
					}
					else if (coordinate == 6)
					{
						button6.Content = symb;
						symbols[coordinate] = anotherTurn ? Symbol.Cross : Symbol.Nought;
					}
					else if (coordinate == 7)
					{
						button7.Content = symb;
						symbols[coordinate] = anotherTurn ? Symbol.Cross : Symbol.Nought;
					}
					else if (coordinate == 8)
					{
						button8.Content = symb;
						symbols[coordinate] = anotherTurn ? Symbol.Cross : Symbol.Nought;
					}
					CheckVictory();
					if (gameOver)
					{
						NewGameInvoke();
					}
				}
				));
		}
		void DisableButton()
		{
			Dispatcher.Invoke(new Action(
				() =>
				{
					button0.IsEnabled = false;
					button1.IsEnabled = false;
					button2.IsEnabled = false;
					button3.IsEnabled = false;
					button4.IsEnabled = false;
					button5.IsEnabled = false;
					button6.IsEnabled = false;
					button7.IsEnabled = false;
					button8.IsEnabled = false;
				}
				));
		}
		void EnableButton()
		{
			Dispatcher.Invoke(new Action(
				() =>
				{
					button0.IsEnabled = true;
					button1.IsEnabled = true;
					button2.IsEnabled = true;
					button3.IsEnabled = true;
					button4.IsEnabled = true;
					button5.IsEnabled = true;
					button6.IsEnabled = true;
					button7.IsEnabled = true;
					button8.IsEnabled = true;
				}
				));
		}		
		#endregion
		#region Условие победы
		private void CheckVictory()
		{
			string winner = null;
			for (int i = 0; i < symbols.Length; i++)
			{
				if (symbols[i]==Symbol.Cross)
				{
					winner = "X";
				}
				else if (symbols[i] == Symbol.Nought)
				{
					winner = "O";
				}
				//победитель по горизонтали
				if (symbols[i] != Symbol.Free && (symbols[0] & symbols[1] & symbols[2]) == symbols[i])
				{
					gameOver = true;
					//button1.Background = button2.Background = button3.Background = Brushes.Green;					
					MessageBox.Show(playerTurnSymbol + " Победитель " + winner); break;
				}
				else if (symbols[i] != Symbol.Free && (symbols[3] & symbols[4] & symbols[5]) == symbols[i])
				{
					gameOver = true;
					MessageBox.Show(playerTurnSymbol + " Победитель " + winner); break;
				}
				else if (symbols[i] != Symbol.Free && (symbols[6] & symbols[7] & symbols[8]) == symbols[i])
				{
					gameOver = true;
					MessageBox.Show(playerTurnSymbol + " Победитель " + winner); break;
				}

				//победитель по вертикали
				else if (symbols[i] != Symbol.Free && (symbols[0] & symbols[3] & symbols[6]) == symbols[i])
				{
					gameOver = true;
					MessageBox.Show(playerTurnSymbol + " Победитель " + winner); break;
				}
				else if (symbols[i] != Symbol.Free && (symbols[1] & symbols[4] & symbols[7]) == symbols[i])
				{
					gameOver = true;
					MessageBox.Show(playerTurnSymbol + " Победитель " + winner); break;
				}
				else if (symbols[i] != Symbol.Free && (symbols[2] & symbols[5] & symbols[8]) == symbols[i])
				{
					gameOver = true;
					MessageBox.Show(playerTurnSymbol + " Победитель " + winner); break;
				}

				//победитель по диагонали
				else if (symbols[i] != Symbol.Free && (symbols[0] & symbols[4] & symbols[8]) == symbols[i])
				{
					gameOver = true;
					MessageBox.Show(playerTurnSymbol + " Победитель " + winner); break;
				}
				else if (symbols[i] != Symbol.Free && (symbols[2] & symbols[4] & symbols[6]) == symbols[i])
				{
					gameOver = true;
					MessageBox.Show(playerTurnSymbol + " Победитель " + winner); break;
				}		
			}

			//ничья
			if (!symbols.Any(f => f == Symbol.Free))
			{
				gameOver = true;
				MessageBox.Show("Ничья");
			}
		}
		#endregion
	}
}
