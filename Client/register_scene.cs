using Godot;
using System;
using System.Net.Mail;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.CompilerServices;
using Nakama;

public partial class register_scene : Control
{
	static private readonly Regex RegexCheckPass = new Regex(@"(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[a-zA-Z]).{8,}");
	static private readonly Godot.Color Green = new Godot.Color(0.0f, 1.0f, 0.0f, 1.0f);
	static private readonly Godot.Color Red = new Godot.Color(1.0f, 0.0f, 0.0f, 1.0f);
	static private Nakama.Client client;
	private bool EmailValid(string email)
	{
		var valid = true;

		try { var emailAddress = new MailAddress(email); }
		catch { valid = false; }

		return valid;
	}
	private bool PasswordValid(string password)
	{
		/*At least one lower case letter
		At least one upper case letter
		At least special character
		At least one number
		At least 8 characters length*/

		return RegexCheckPass.IsMatch(password);
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		const string Scheme = "http";
		const string Host = "127.0.0.1";
		const int Port = 7350;
		const string ServerKey = "defaultkey";
		client = new Nakama.Client(Scheme, Host, Port, ServerKey);
		client.Timeout = 10;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) { }

	private void _on_back_button_pressed()
	{
		GetTree().ChangeSceneToFile("res://main_menu.tscn");
	}
	private async void _on_register_button_pressed()
	{
		GetNode<Button>("VBoxContainer/RegisterButton").Disabled = true;

		string Email = GetNode<LineEdit>("VBoxContainer/EmailBox").Text;
		string UserName = GetNode<LineEdit>("VBoxContainer/UsernameBox").Text;
		string Password = GetNode<LineEdit>("VBoxContainer/PasswordBox").Text;

		if (EmailValid(Email) && PasswordValid(Password))
		{
			try
			{
				var session = await client.AuthenticateEmailAsync(Email, Password, UserName);
				GD.Print(session);
			}
			catch
			{
				//do something???
			}
		}
		GetNode<Button>("VBoxContainer/RegisterButton").Disabled = false;
	}
}
