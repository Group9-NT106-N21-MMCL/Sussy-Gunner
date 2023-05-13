using System;
using Godot;
using Nakama;
public partial class ClientNode : Node
{
    [Serializable]
    public class PlayerState
    {
        public bool isDirection = false;
        public float PosX = 0, PosY = 0;
        public float GunRoate = 0;
        public bool GunFlip = false;
    }
    private const string Scheme = "http";
    private const string Host = "100.91.95.109";
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
