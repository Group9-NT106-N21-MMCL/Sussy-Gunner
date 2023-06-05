using Godot;
using System.Threading.Tasks;
using Nakama;
using System.Linq;
using Chickensoft.GoDotNet;
using Nakama.TinyJson;

public partial class dashboard : Control
{
    private ClientNode ClientNode => this.Autoload<ClientNode>();
    static private IMatch match = null;
    // Called when the node enters the scene tree for the first time.
    static public IMatch GetMatch() => match;
    public override void _Ready()
    {
        var Username = ClientNode.Session.Username;
        var ID = ClientNode.Session.UserId;
        var UserInforNode = GetNode<Control>("TextureButton/UserInfo");
        UserInforNode.GetNode<LineEdit>("ID").Text = ID;
        UserInforNode.GetNode<LineEdit>("Username").Text = Username;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
    private async void _on_create_game_button_pressed()
    {
        match = null;
        var MatchName = GetNode<LineEdit>("MarginContainer2/VBoxContainer/RoomName").Text;
        match = await ClientNode.Socket.CreateMatchAsync(MatchName);
        if (match.Presences.ToList().Count() == 0) //Room just have only you -> Creat match
            GetTree().ChangeSceneToFile("res://scenes/map.tscn");
        else //Room exist
        {
            await ClientNode.Socket.LeaveMatchAsync(match.Id);
            var opCode = 5;
            await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson("Left!"));
            var Red = new Godot.Color("#FF0000");
            GetNode<LineEdit>("MarginContainer2/VBoxContainer/RoomName").Set("theme_override_colors/font_color", Red);
        }
    }

    private async void _on_join_game_button_pressed()
    {
        match = null;
        var MatchName = GetNode<LineEdit>("MarginContainer2/VBoxContainer/RoomName").Text;
        match = await ClientNode.Socket.CreateMatchAsync(MatchName);
        var NumPlayer = match.Presences.ToList().Count();
        if (NumPlayer == 0 || NumPlayer >= 4) //Room just have only you or run out of slot -> Room do not exit
        {
            await ClientNode.Socket.LeaveMatchAsync(match.Id);
            var Red = new Godot.Color("#FF0000");
            GetNode<LineEdit>("MarginContainer2/VBoxContainer/RoomName").Set("theme_override_colors/font_color", Red);
        }
        else GetTree().ChangeSceneToFile("res://scenes/map.tscn");
    }

    private async void _on_texture_button_pressed()
    {
        var userInfo = GetNode<Control>("TextureButton/UserInfo");
        userInfo.Visible = !userInfo.Visible;
    }

}
