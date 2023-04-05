using Godot;
using System;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

public partial class register_scene : Control
{
	static private readonly string sheet = "Information";
	static private readonly string SpreadsheetId = "1t8R5uEUzoxq2l8WgiXQoqnzVMoaOXaXA1kuDWAAQwCE";
	static private readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
	static private readonly string ApplicationName = "Sussy Gunner";
	static private SheetsService service; 
	
	private void AddRow(string Email, string Username, string Password)
	{	
		// Specifying Column Range for reading...
		var range = $"{sheet}!A:C";
		var valueRange = new ValueRange();

		var oblist = new List<object>();
		oblist.Add(Email); oblist.Add(Username); oblist.Add(Password);

		valueRange.Values = new List<IList<object>> { oblist };

		// Append the above record...
		var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
		appendRequest.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
		appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
   
		var appendReponse = appendRequest.Execute();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready(){
		var credential = GoogleCredential.FromStream(new System.IO.FileStream("DatabaseAPI.json", System.IO.FileMode.Open)).CreateScoped(Scopes);
		service = new SheetsService(new BaseClientService.Initializer()
		{
			HttpClientInitializer = credential,
			ApplicationName = ApplicationName,
		});
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta){}

	private void _on_back_button_pressed()
	{
		GetTree().ChangeSceneToFile("res://main_menu.tscn");
	}
	private void _on_login_button_pressed()
	{
		string Email = GetNode<LineEdit>("VBoxContainer/EmailBox").Text;
		string UserName = GetNode<LineEdit>("VBoxContainer/UsernameBox").Text;
		string Password = GetNode<LineEdit>("VBoxContainer/PasswordBox").Text;
		AddRow(Email, UserName, Password);
	}
}





