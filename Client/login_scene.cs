using Godot;
using System;
using System.Net.Mail;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Diagnostics.Tracing;

public partial class login_scene : Control
{
	static private readonly string sheet = "Information";
	static private readonly string SpreadsheetId = "1t8R5uEUzoxq2l8WgiXQoqnzVMoaOXaXA1kuDWAAQwCE";
	static private readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
	static private readonly string ApplicationName = "Sussy Gunner";

	static private SheetsService service;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var credential = GoogleCredential.FromStream(new System.IO.FileStream("DatabaseAPI.json", System.IO.FileMode.Open)).CreateScoped(Scopes);
		service = new SheetsService(new BaseClientService.Initializer()
		{
			HttpClientInitializer = credential,
			ApplicationName = ApplicationName,
		});
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private void _on_back_button_pressed()
	{
		GetTree().ChangeSceneToFile("res://main_menu.tscn");
	}
	private void _on_login_button_pressed()
	{
		string UserName = GetNode<LineEdit>("VBoxContainer/UsernameBox").Text;
		string Password = GetNode<LineEdit>("VBoxContainer/PasswordBox").Text;
		String range = $"{sheet}!A:C";
		SpreadsheetsResource.ValuesResource.GetRequest request =
				service.Spreadsheets.Values.Get(SpreadsheetId, range);
		ValueRange response = request.Execute();
		IList<IList<Object>> values = response.Values;
		if (values != null && values.Count > 0)
		{
			foreach (var row in values)
			{
				string user = row[1].ToString();
				string password = row[2].ToString();
				if (user == UserName &&  password == Password)
				{
					GD.Print("DUMA DC ROI NE");
					GetTree().ChangeSceneToFile("res://main_menu.tscn");
				}
			}
		}
	}
}






