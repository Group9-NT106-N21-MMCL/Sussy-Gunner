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
	static private readonly Godot.Color White = new Godot.Color("#FFFFFF");
	static private readonly Godot.Color Green = new Godot.Color("#7CFC00");
	static private readonly Godot.Color Red = new Godot.Color("#FF0000");
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
		const string Host = "dan-laptop.penguin-major.ts.net";
		const int Port = 7350;
		const string ServerKey = "defaultkey";
		client = new Nakama.Client(Scheme, Host, Port, ServerKey);
		client.Timeout = 10;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) { }

	private void _on_back_button_pressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
	}
	private async void _on_register_button_pressed()
	{
		GetNode<Button>("VBoxContainer/RegisterButton").Disabled = true;
		var EmailNode = GetNode<LineEdit>("VBoxContainer/EmailBox");
		var UserNameNode = GetNode<LineEdit>("VBoxContainer/UsernameBox");
		var PasswordNode = GetNode<LineEdit>("VBoxContainer/PasswordBox");

		string Email = EmailNode.Text;
		string UserName = UserNameNode.Text;
		string Password = PasswordNode.Text;

		if (!EmailValid(Email))
		{
			EmailNode.Set("theme_override_colors/font_color", Red);
			GD.Print("Invalid email!");
			GetNode<Button>("VBoxContainer/RegisterButton").Disabled = false;
			return;
		}


		if (!PasswordValid(Password))
		{
			PasswordNode.Set("theme_override_colors/font_color", Red);
			GD.Print("Invalid password!");
			GetNode<Button>("VBoxContainer/RegisterButton").Disabled = false;
			return;
		}

		try
		{
			//Try to login
			var LoginSession = await client.AuthenticateEmailAsync(Email, Password, UserName, create: false);
			GD.Print("Account exist!");
			EmailNode.Set("theme_override_colors/font_color", Red);
			await client.SessionLogoutAsync(LoginSession);
		}
		catch (ApiResponseException e)
		{
			switch (e.GrpcStatusCode)
			{
				case 16:
					GD.Print("This email was used in another account!");
					EmailNode.Set("theme_override_colors/font_color", Red);
					break;
				case 5:
					try
					{
						var RegSession = await client.AuthenticateEmailAsync(Email, Password, UserName, create: true);
						EmailNode.Set("theme_override_colors/font_color", Green);
						UserNameNode.Set("theme_override_colors/font_color", Green);
						PasswordNode.Set("theme_override_colors/font_color", Green);
						GD.Print("Account successfully registered!");
					}
					catch (ApiResponseException f)
					{
						if (f.GrpcStatusCode == 6)
						{
							UserNameNode.Set("theme_override_colors/font_color", Red);
							GD.Print("This username was used in another account!");
						}
					}
					break;
			}
		}
		GetNode<Button>("VBoxContainer/RegisterButton").Disabled = false;
	}
	private void _on_email_box_text_changed(string new_text)
	{
		GetNode<LineEdit>("VBoxContainer/EmailBox").Set("theme_override_colors/font_color", White);
	}
	private void _on_username_box_text_changed(string new_text)
	{
		GetNode<LineEdit>("VBoxContainer/UsernameBox").Set("theme_override_colors/font_color", White);
	}
	private void _on_password_box_text_changed(string new_text)
	{
		GetNode<LineEdit>("VBoxContainer/PasswordBox").Set("theme_override_colors/font_color", White);
	}
}








