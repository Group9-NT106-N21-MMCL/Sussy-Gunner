using Godot;
using Nakama;

public partial class ClientNode : Node
{
    private const string Scheme = "http";
    private const string Host = "dan-laptop-docker-desktop.penguin-major.ts.net";
    private const int Port = 7350;
    private const string ServerKey = "defaultkey";
    public IClient? Client;
    public ISocket? Socket;
    public ISession? Session;

    public override void _Ready()
    {
        Client = new Nakama.Client(Scheme, Host, Port, ServerKey);
        Client.Timeout = 10;
        Socket = Nakama.Socket.From(Client);
    }

}