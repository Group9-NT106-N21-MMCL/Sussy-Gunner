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
            switch (matchState.OpCode)
            {
                case 0: //Get position
                    var stateJson = Encoding.UTF8.GetString(matchState.State);
                    var state = JsonParser.FromJson<ClientNode.PositionState>(stateJson);
                    var coordinate = new Vector2(state.X, state.Y);

                    var UserID = matchState.UserPresence.UserId;
                    var Username = matchState.UserPresence.Username;

                    if (players.ContainsKey(UserID) && state.isDirection)
                        await players[UserID].Move(coordinate);
                    else if (!players.ContainsKey(UserID) && !state.isDirection)
                        await SpawPlayer(UserID, Username, coordinate);
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
            var state = new ClientNode.PositionState { isDirection = false, X = SelfPos.X, Y = SelfPos.Y };

            var opCode = 0; //Send position
            await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state));
        }
    }
}
