using Godot;
using System;
using Nakama;
using System.Linq;

public partial class dashboard : Control
{
	private ISocket socket;
	private ISession session;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		socket = login_scene.GetSocket();
		session = login_scene.GetSession();
		var Username = session.Username;
		GetNode<Label>("MarginContainer/VBoxContainer/Username").Text = "Username: " + Username;

		GD.Print("Username: ", Username);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) { }
	private async void _on_create_game_button_pressed()
	{
		var MatchName = GetNode<LineEdit>("MarginContainer2/VBoxContainer/RoomName").Text;
		var match = await socket.CreateMatchAsync(MatchName);
		if (match.Presences.ToList().Count() == 0) //Room just have only you -> Creat match
			GetTree().ChangeSceneToFile("res://scenes/map.tscn");
		else //Room exist
		{
			await socket.LeaveMatchAsync(match.Id);
			var Red = new Godot.Color("#FF0000");
			GetNode<LineEdit>("MarginContainer2/VBoxContainer/RoomName").Set("theme_override_colors/font_color", Red);
		}
	}

	private async void _on_join_game_button_pressed()
	{
		var MatchName = GetNode<LineEdit>("MarginContainer2/VBoxContainer/RoomName").Text;
		var match = await socket.CreateMatchAsync(MatchName);
		GD.Print(match.Presences.ToList().Count());
		if (match.Presences.ToList().Count() == 0) //Room just have only you -> Room do not exit
		{
			await socket.LeaveMatchAsync(match.Id);
			var Red = new Godot.Color("#FF0000");
			GetNode<LineEdit>("MarginContainer2/VBoxContainer/RoomName").Set("theme_override_colors/font_color", Red);
		}
		else GetTree().ChangeSceneToFile("res://scenes/map.tscn");
	}
}
