using Godot;
using System;
using Nakama;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chickensoft.GoDotNet;
using Nakama.TinyJson;
using System.Text;
using System.Linq;

public partial class map : Node2D
{
    private IMatch match = dashboard.GetMatch();
    private ClientNode ClientNode => this.Autoload<ClientNode>();
    private Dictionary<string, player> players = new Dictionary<string, player>();
    private string HostID = "";
    ItemList ChatFrame;
    Control MatchKD;
    PackedScene scene;
    double DelayTime = 0.0, SpawnTime = 0.0;
    public async Task SpawPlayer(string UserID, string Username, Vector2 Pos, bool isHost)
    {
        var _player = scene.Instantiate<player>();
        _player.Scale = new Vector2((float)0.5, (float)0.5);
        _player.Name = UserID;
        _player.isHost = isHost;
        if (isHost) HostID = UserID;
        _player.GetNode<Label>("PlayerName").Text = Username;
        _player.Position = Pos;
        _player.SetMatch(match);
        AddChild(_player);
        players.Add(UserID, _player);
    }
    public async Task RemovePlayer(string UserID)
    {
        var _player = GetNode<player>(UserID);
        RemoveChild(_player);
        _player.QueueFree();
        players.Remove(UserID);
    }
    private async void ReceivedMatchState(IMatchState matchState)
    {
        var UserID = matchState.UserPresence.UserId;
        var Username = matchState.UserPresence.Username;
        var JsonData = Encoding.UTF8.GetString(matchState.State);
        switch (matchState.OpCode)
        {
            case 0: //Move player
                var state = JsonParser.FromJson<ClientNode.PlayerState>(JsonData);
                var direction = new Vector2(state.PosX, state.PosY);
                if (players.ContainsKey(UserID))
                {
                    float GunRoate = state.GunRoate;
                    bool GunFlip = state.GunFlip;
                    if (state.isDirection)
                    {
                        await players[UserID].Move(direction, GunRoate, GunFlip);
                        players[UserID].SetHealth(state.Health);
                    }
                    else
                    {
                        players[UserID].Stop();
                        players[UserID].Position = direction;
                        players[UserID].SetGunRotate(GunRoate);
                        players[UserID].SetFlip(GunFlip);
                        players[UserID].SetHealth(state.Health);
                    }
                }
                break;
            case 1:
                var mess = JsonParser.FromJson<string>(JsonData);
                break;

            case 2: //Someone is shooting
                var ShootState = JsonParser.FromJson<ClientNode.PlayerState>(JsonData);
                players[UserID].SetGunRotate(ShootState.GunRoate);
                players[UserID].SetFlip(ShootState.GunFlip);
                await players[UserID].Shoot(UserID, ShootState.GunRoate);
                break;

            case 3: //Someone dead or still alive
                var LiveOrDead = JsonParser.FromJson<String>(JsonData);

                if (LiveOrDead == "Dead!")
                {
                    await players[UserID].DeadOrLive();
                    players[UserID].SetHealth(0);
                    players[UserID].IncDead();
                }
                else if (LiveOrDead == "Live!")
                {
                    await players[UserID].DeadOrLive(true);
                    players[UserID].SetHealth(10);
                }
                break;

            case 4: //Spawn player
                var PosState = JsonParser.FromJson<ClientNode.PlayerState>(JsonData);
                var Pos = new Vector2(PosState.PosX, PosState.PosY);
                if (!players.ContainsKey(UserID))
                {
                    await SpawPlayer(UserID, Username, Pos, PosState.isHost);
                    var SelfID = ClientNode.Session.UserId;

                    var opCode = 4; //Send position to you player to spawn him
                    var PlayerState = new ClientNode.PlayerState { PosX = players[SelfID].Position.X, PosY = players[SelfID].Position.Y };
                    await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(PlayerState));
                }
                break;

            case 5: //Someone left
                var NewHostID = JsonParser.FromJson<string>(JsonData);
                await RemovePlayer(UserID);
                if (ClientNode.Session.UserId == NewHostID)
                {
                    ClientNode.isHost = true;
                    players[NewHostID].isHost = true;
                }
                break;

            case 6: //Chatting
                var Message = JsonParser.FromJson<string>(JsonData);
                ChatFrame.AddItem($"{Username}: {Message}");
                break;

            case 7: //Someone has kill somebody!
                var IDWhoShoot = JsonParser.FromJson<string>(JsonData);
                players[IDWhoShoot].IncKill();
                break;

            default:
                GD.Print("Unsupported op code");
                break;
        }
    }
    // Called when the node enters the scene tree for the first time.
    public async override void _Ready()
    {
        Name = ClientNode.Session.UserId;
        GetNode<Chat_Box>("Quit/ChatButton/Chat_Box").SetMatch(match);
        ClientNode.Socket.ReceivedMatchState += ReceivedMatchState;
        ChatFrame = GetNode<Chat_Box>("Quit/ChatButton/Chat_Box").GetNode<ItemList>("ChatFrame");
        MatchKD = GetNode<Control>("TabBoard/Tab");
        scene = GD.Load<PackedScene>("res://character/player.tscn");

        var SelfID = ClientNode.Session.UserId;
        var SelfName = ClientNode.Session.Username;
        var SelfPos = new Vector2(0.0f, 0.0f);

        await SpawPlayer(SelfID, SelfName, SelfPos, ClientNode.isHost);
        var opCode = 4; //Send position to another player to spawn me
        var state = new ClientNode.PlayerState { PosX = SelfPos.X, PosY = SelfPos.Y, isHost = ClientNode.isHost };
        await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state));
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public async override void _PhysicsProcess(double delta)
    {
        if (!players.ContainsKey(ClientNode.Session.UserId)) return;

        if (ClientNode.isHost)
            GD.Print($"{ClientNode.Session.Username} is a host of this room!");
        var ammoAmount = players[ClientNode.Session.UserId].GetAmmor();
        foreach (var player in players.Values)
            await player.DisplayHealth();

        SpawnTime += delta;
        if (ClientNode.isHost && SpawnTime >= 5)
        {
            SpawnTime = 0;
            var opCode = 1;
            await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson("Hello"));
        }

        GetNode<Label>("RemainingBullet/BulletNumber").Text = $"{ammoAmount}/20";
        DelayTime += delta;
        if (DelayTime >= 1)
        {
            if (Input.IsActionPressed("tab"))
            {
                List<KeyValuePair<int, player>> DashBoard = new List<KeyValuePair<int, player>>();

                foreach (var User in players.Values)
                {
                    var Score = User.GetKill() - User.GetDead();
                    var Input = new KeyValuePair<int, player>(Score, User);
                    DashBoard.Add(Input);
                }
                DashBoard = DashBoard.OrderByDescending(x => x.Key).ToList();
                int d = 1;
                foreach (var User in DashBoard)
                {
                    MatchKD.GetNode<Label>($"Names/Player{d}_Name").Text = User.Value.GetNode<Label>("PlayerName").Text;
                    MatchKD.GetNode<Label>($"KillDead/Player_Kill{d}").Text = User.Value.GetKill().ToString();
                    MatchKD.GetNode<Label>($"KillDead/Player_Dead{d}").Text = User.Value.GetDead().ToString();
                    MatchKD.Visible = true;
                    ++d;
                }
            }
            else MatchKD.Visible = false;

            if (Input.IsActionPressed("enter"))
            {
                var ChatBox = GetNode<Chat_Box>("Quit/ChatButton/Chat_Box");
                if (ChatBox.Visible)
                {
                    if (ChatBox.GetNode<LineEdit>("TextBox").Text != "")
                        ChatBox._on_button_pressed();
                    else ChatBox.Visible = !ChatBox.Visible;
                }
                else
                {
                    ChatBox.Visible = !ChatBox.Visible;
                    ChatBox.GetNode<LineEdit>("TextBox").GrabFocus();
                }
            }
            if (Input.IsActionPressed("quit"))
                _on_quit_button_pressed();
            DelayTime = 0;
        }
    }
    public void _on_quit_button_pressed()
    {
        var quit = GetNode<CanvasLayer>("QuitComponent");
        quit.Visible = !quit.Visible;
    }

    public async void _on_yes_button_pressed()
    {
        string NewIDHost = "";

        if (ClientNode.isHost)
            NewIDHost = players.Keys.ElementAt(1);

        players.Clear();
        ClientNode.isHost = false;
        ClientNode.Socket.ReceivedMatchState -= ReceivedMatchState;
        var opCode = 5;
        await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(NewIDHost));
        await ClientNode.Socket.LeaveMatchAsync(match.Id);
        GetTree().ChangeSceneToFile("res://scenes/dashboard.tscn");
    }

    public void _on_no_button_pressed()
    {
        var quit = GetNode<CanvasLayer>("QuitComponent");
        quit.Visible = !quit.Visible;
    }

    private async void _on_chat_button_pressed()
    {
        var ChatBox = GetNode<Chat_Box>("Quit/ChatButton/Chat_Box");
        ChatBox.Visible = !ChatBox.Visible;
    }
}
