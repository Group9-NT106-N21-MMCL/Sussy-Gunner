using Godot;
using System;
using Nakama;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chickensoft.GoDotNet;

public partial class map : Node2D
{
	private IMatch match = dashboard.GetMatch();
	private ClientNode ClientNode => this.Autoload<ClientNode>();
	private Dictionary<string, Player> players = new Dictionary<string, Player>();

	public async Task SpawPlayer(string sessionID)
	{
		var scene = GD.Load<PackedScene>("res://character/Player.tscn");
		var player = scene.Instantiate<Player>();
		player.Scale = new Vector2((float)0.5, (float)0.5);
		AddChild(player);
		players.Add(sessionID, player);
	}
	// Called when the node enters the scene tree for the first time.
	public override async void _Ready() { }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override async void _Process(double delta)
	{
		if (!players.ContainsKey(ClientNode.Session.UserId))
			await SpawPlayer(ClientNode.Session.UserId);
	}
}
