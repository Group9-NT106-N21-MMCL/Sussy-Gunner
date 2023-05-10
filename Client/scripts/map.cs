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
	private Dictionary<string, Player> players = new Dictionary<string, Player>();

	public async Task SpawPlayer(string UserID, string Username)
	{
		var scene = GD.Load<PackedScene>("res://character/Player.tscn");
		var player = scene.Instantiate<Player>();
		player.Scale = new Vector2((float)0.5, (float)0.5);
		player.Name = Username;
		AddChild(player);
		players.Add(UserID, player);
	}
	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		var SelfID = ClientNode.Session.UserId;
		var SelfName = ClientNode.Session.Username;

		await SpawPlayer(SelfID, SelfName);
		ClientNode.Socket.ReceivedMatchPresence += async matchPresenceEvent =>
		{
			foreach (var presence in matchPresenceEvent.Joins)
				if (!players.ContainsKey(presence.UserId))
					await SpawPlayer(presence.UserId, presence.Username);
		};

		ClientNode.Socket.ReceivedMatchState += async matchState =>
		{
			switch (matchState.OpCode)
			{
				case 0: //Get position
					var stateJson = Encoding.UTF8.GetString(matchState.State);
					var positionState = JsonParser.FromJson<Vector2>(stateJson);

					if (players.ContainsKey(matchState.UserPresence.UserId))
					{
						var _player = players[matchState.UserPresence.UserId];
						await _player.Move(positionState);
					}
					break;
				case 1:
					var S = Encoding.UTF8.GetString(matchState.State);
					GD.Print(S);
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

			var opCode = 1; //Send position
			await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson("ABC"));
		}
	}
}
