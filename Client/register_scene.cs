using Godot;
using System;
using System.Net.Mail;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

public partial class register_scene : Control
{
	static private readonly string sheet = "Information";
	static private readonly string range = $"{sheet}!A:C";
	static private readonly string SpreadsheetId = "1t8R5uEUzoxq2l8WgiXQoqnzVMoaOXaXA1kuDWAAQwCE";
	static private readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
	static private readonly string ApplicationName = "Sussy Gunner";
	static private readonly Dictionary<string, string> Database = new Dictionary<string, string> {};
	static private readonly Regex RegexCheckPass = new Regex(@"(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[a-zA-Z]).{8,}");
	static private readonly Godot.Color Green = new Godot.Color(0.0f, 1.0f, 0.0f, 1.0f);
	static private readonly Godot.Color Red = new Godot.Color(1.0f, 0.0f, 0.0f, 1.0f);

	static private SheetsService service;
	private string HashPassword(string password)
	{
		const int keySize = 64;
		const int iterations = 350000;
		var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(keySize);
		var hash = Rfc2898DeriveBytes.Pbkdf2(
			Encoding.UTF8.GetBytes(password),
			salt,
			iterations,
			HashAlgorithmName.SHA512,
			keySize);
		return Convert.ToHexString(hash);
	}
	private void AddRow(string Email, string Username, string Password)
	{
		// Specifying Column Range for reading...
		var valueRange = new ValueRange();

		var oblist = new List<object>();
		oblist.Add(Email); oblist.Add(Username); oblist.Add(Password);
		Database.Add(Username, Password);

		valueRange.Values = new List<IList<object>> { oblist };

		// Append the above record...
		var appendRequest = service.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
		appendRequest.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
		appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;

		var appendReponse = appendRequest.Execute();
	}
	private bool EmailValid(string email)
	{
		var valid = true;

		try { var emailAddress = new MailAddress(email); }
		catch { valid = false; }

		return valid;
	}
	private bool UsernameValid(string username)
	{
		return !Database.ContainsKey(username);
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
		var credential = GoogleCredential.FromStream(new System.IO.FileStream("DatabaseAPI.json", System.IO.FileMode.Open)).CreateScoped(Scopes);
		service = new SheetsService(new BaseClientService.Initializer()
		{
			HttpClientInitializer = credential,
			ApplicationName = ApplicationName,
		});

		SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(SpreadsheetId, range);
		ValueRange response = request.Execute();

		IList<IList<Object>> values = response.Values;
		if (values != null && values.Count > 0)
		{
			foreach (var row in values)
				Database.Add(row[1].ToString(), row[2].ToString());
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) { }

	private void _on_back_button_pressed()
	{
		GetTree().ChangeSceneToFile("res://main_menu.tscn");
	}
	private void _on_register_button_pressed()
	{
		string Green = "(0, 1, 0, 1)";

		string Email = GetNode<LineEdit>("VBoxContainer/EmailBox").Text;
		bool EmailOK = (GetNode<LineEdit>("VBoxContainer/EmailBox").Get("theme_override_colors/font_color").ToString() == Green);

		string UserName = GetNode<LineEdit>("VBoxContainer/UsernameBox").Text;
		bool UsernameOK = (GetNode<LineEdit>("VBoxContainer/UsernameBox").Get("theme_override_colors/font_color").ToString() == Green);
		
		string Password = GetNode<LineEdit>("VBoxContainer/PasswordBox").Text;
		bool PasswordOK = (GetNode<LineEdit>("VBoxContainer/PasswordBox").Get("theme_override_colors/font_color").ToString() == Green);
		
		if (EmailOK && UsernameOK && PasswordOK) 
			AddRow(Email, UserName, HashPassword(Password));
	}
	private void _on_email_box_text_changed(string new_text)
	{
		if (EmailValid(new_text))
			GetNode<LineEdit>("VBoxContainer/EmailBox").Set("theme_override_colors/font_color", Green);
		else
			GetNode<LineEdit>("VBoxContainer/EmailBox").Set("theme_override_colors/font_color", Red);
	}
	private void _on_username_box_text_changed(string new_text)
	{
		if (UsernameValid(new_text))
			GetNode<LineEdit>("VBoxContainer/UsernameBox").Set("theme_override_colors/font_color", Green);
		else
			GetNode<LineEdit>("VBoxContainer/UsernameBox").Set("theme_override_colors/font_color", Red);
	}
	private void _on_password_box_text_changed(string new_text)
	{
		if (PasswordValid(new_text))
			GetNode<LineEdit>("VBoxContainer/PasswordBox").Set("theme_override_colors/font_color", Green);
		else
			GetNode<LineEdit>("VBoxContainer/PasswordBox").Set("theme_override_colors/font_color", Red);
	}
}
