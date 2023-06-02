using Godot;
using System;
using Nakama;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chickensoft.GoDotNet;
using Nakama.TinyJson;
using System.Text;

public partial class map : Node2D
{

    private IMatch match = dashboard.GetMatch();
    private ClientNode ClientNode => this.Autoload<ClientNode>();
    private Dictionary<string, player> players = new Dictionary<string, player>();

    public async Task SpawPlayer(string UserID, string Username, Vector2 Pos)
    {
        var scene = GD.Load<PackedScene>("res://character/player.tscn");
        var _player = scene.Instantiate<player>();
        _player.Scale = new Vector2((float)0.5, (float)0.5);
        _player.Name = Username;
        _player.GetNode<Area2D>("Area").Name = "Player_" + UserID;
        _player.Position = Pos;
        _player.SetMatch(match);
        AddChild(_player);
        players.Add(UserID, _player);
    }
    // Called when the node enters the scene tree for the first time.
    public override async void _Ready()
    {
        var SelfID = ClientNode.Session.UserId;
        var SelfName = ClientNode.Session.Username;
        await SpawPlayer(SelfID, SelfName, new Vector2(0.0f, 0.0f));

        ClientNode.Socket.ReceivedMatchState += async matchState =>
        {
            var UserID = matchState.UserPresence.UserId;
            var Username = matchState.UserPresence.Username;
            switch (matchState.OpCode)
            {
                case 0: //Get position
                    var stateJson = Encoding.UTF8.GetString(matchState.State);
                    var state = JsonParser.FromJson<ClientNode.PlayerState>(stateJson);
                    var coordinate = new Vector2(state.PosX, state.PosY);

                    if (players.ContainsKey(UserID))
                    {
                        if (state.isDirection)
                        {
                            float GunRoate = state.GunRoate;
                            bool GunFlip = state.GunFlip;
                            await players[UserID].Move(coordinate, GunRoate, GunFlip, UserID);
                        }
                        else players[UserID].Position = coordinate;
                    }
                    else
                        await SpawPlayer(UserID, Username, coordinate);
                    break;
                case 1: //Someone shot!
                    var JsonShot = Encoding.UTF8.GetString(matchState.State);
                    var UserIDShot = JsonParser.FromJson<String>(JsonShot);
                    players[UserIDShot].DecHealth();
                    break;
                case 2:
                    await players[UserID].Shoot();
                    break;
                case 3:
                    var LiveOrDeadData = Encoding.UTF8.GetString(matchState.State);
                    var LiveOrDead = JsonParser.FromJson<String>(LiveOrDeadData);

                    if (LiveOrDead == "Dead!")
                    {
                        await players[UserID].DeadOrLive(UserID);
                        players[UserID].SetHealth(0);
                    }

                    else if (LiveOrDead == "Live!")
                    {
                        await players[UserID].DeadOrLive(UserID, true);
                        players[UserID].SetHealth(10);
                    }

                    break;

                default:
                    GD.Print("Unsupported op code");
                    break;
            }
        };
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override async void _Process(double delta)
    {
        if (players.ContainsKey(ClientNode.Session.UserId))
        {
            var SelfPos = players[ClientNode.Session.UserId].Position;
            var state = new ClientNode.PlayerState { isDirection = false, PosX = SelfPos.X, PosY = SelfPos.Y };

            var opCode = 0; //Send position
            await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state));
        }
    }

    public void _on_quit_button_pressed()
    {
        var quit = GetNode<CanvasLayer>("QuitComponent");
        quit.Visible = !quit.Visible;
    }

    public async void _on_yes_button_pressed()
    {
        await ClientNode.Socket.LeaveMatchAsync(match.Id);
        GetTree().ChangeSceneToFile("res://scenes/dashboard.tscn");
    }

    public void _on_no_button_pressed()
    {
        var quit = GetNode<CanvasLayer>("QuitComponent");
        quit.Visible = false;
    }
}
