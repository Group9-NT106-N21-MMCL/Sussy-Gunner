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
    public async Task RemovePlayer(string UserID, string Username)
    {
        var _player = GetNode<player>(Username);
        RemoveChild(_player);
        _player.QueueFree();
        players.Remove(UserID);
    }
    // Called when the node enters the scene tree for the first time.
    public override async void _Ready()
    {
        var SelfID = ClientNode.Session.UserId;
        var SelfName = ClientNode.Session.Username;
        var SelfPos = new Vector2(0.0f, 0.0f);

        await SpawPlayer(SelfID, SelfName, SelfPos);
        var opCode = 4; //Send position to another player to spawn me
        var state = new ClientNode.PlayerState { PosX = SelfPos.X, PosY = SelfPos.Y };
        Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state)));

        ClientNode.Socket.ReceivedMatchState += async matchState =>
        {
            var UserID = matchState.UserPresence.UserId;
            var Username = matchState.UserPresence.Username;
            switch (matchState.OpCode)
            {
                case 0: //Move player
                    var stateJson = Encoding.UTF8.GetString(matchState.State);
                    var state = JsonParser.FromJson<ClientNode.PlayerState>(stateJson);
                    var direction = new Vector2(state.PosX, state.PosY);
                    if (players.ContainsKey(UserID))
                    {
                        float GunRoate = state.GunRoate;
                        bool GunFlip = state.GunFlip;
                        if (state.isDirection)
                            await players[UserID].Move(direction, GunRoate, GunFlip, UserID);
                        else
                        {
                            players[UserID].Stop();
                            players[UserID].Position = direction;
                            players[UserID].SetGunRotate(GunRoate);
                            players[UserID].SetFlip(GunFlip);
                        }
                    }
                    break;
                case 1: //Someone has been shot!
                    var JsonShot = Encoding.UTF8.GetString(matchState.State);
                    var UserIDShot = JsonParser.FromJson<String>(JsonShot);
                    players[UserIDShot].DecHealth();
                    break;
                case 2: //Someone is shooting
                    var ShootJson = Encoding.UTF8.GetString(matchState.State);
                    var ShootState = JsonParser.FromJson<ClientNode.PlayerState>(ShootJson);
                    players[UserID].SetGunRotate(ShootState.GunRoate);
                    players[UserID].SetFlip(ShootState.GunFlip);
                    await players[UserID].Shoot(ShootState.GunRoate);
                    break;
                case 3: //Someone dead or still alive
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
                case 4: //Spawn player
                    var PosJson = Encoding.UTF8.GetString(matchState.State);
                    var PosState = JsonParser.FromJson<ClientNode.PlayerState>(PosJson);
                    var Pos = new Vector2(PosState.PosX, PosState.PosY);
                    if (!players.ContainsKey(UserID))
                    {
                        await SpawPlayer(UserID, Username, Pos);
                        var opCode = 4; //Send position to you player to spawn him
                        var PlayerState = new ClientNode.PlayerState { PosX = players[SelfID].Position.X, PosY = players[SelfID].Position.Y };
                        Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(PlayerState)));
                    }
                    break;
                case 5: //Someone left
                    await RemovePlayer(UserID, Username);
                    break;
                default:
                    GD.Print("Unsupported op code");
                    break;
            }
        };
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override async void _Process(double delta) { }

    public void _on_quit_button_pressed()
    {
        var quit = GetNode<CanvasLayer>("QuitComponent");
        quit.Visible = !quit.Visible;
    }

    public async void _on_yes_button_pressed()
    {
        players.Clear();
        var opCode = 5;
        await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson("Left!"));
        await ClientNode.Socket.LeaveMatchAsync(match.Id);
        GetTree().ChangeSceneToFile("res://scenes/dashboard.tscn");
    }

    public void _on_no_button_pressed()
    {
        var quit = GetNode<CanvasLayer>("QuitComponent");
        quit.Visible = !quit.Visible;
    }
}
