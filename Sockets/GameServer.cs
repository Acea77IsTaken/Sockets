using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class GameServer
{
    private TcpListener _listener;
    private TcpClient[] _players = new TcpClient[2];
    private bool _isRunning = true;

    public void Start(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        Console.WriteLine("Servidor iniciado. Esperando jugadores...");

        // Aceptar 2 jugadores
        for (int i = 0; i < 2; i++)
        {
            _players[i] = _listener.AcceptTcpClient();
            Console.WriteLine($"Jugador {i + 1} conectado.");
            SendToClient(_players[i], $"CONNECT|{i + 1}"); // Envía ID de jugador
        }

        // Hilo para manejar comunicación
        new Thread(() => HandleCommunication()).Start();
    }

    private void HandleCommunication()
    {
        while (_isRunning)
        {
            for (int i = 0; i < 2; i++)
            {
                if (_players[i].Available > 0)
                {
                    string message = ReceiveFromClient(_players[i]);
                    Console.WriteLine($"Jugador {i + 1}: {message}");
                    // Reenviar mensaje al otro jugador
                    SendToClient(_players[1 - i], message);
                }
            }
            Thread.Sleep(100);
        }
    }

    private string ReceiveFromClient(TcpClient client)
    {
        byte[] buffer = new byte[1024];
        int bytesRead = client.GetStream().Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    private void SendToClient(TcpClient client, string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        client.GetStream().Write(buffer, 0, buffer.Length);
    }
}