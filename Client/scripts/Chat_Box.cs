using Godot;
using Chickensoft.GoDotNet;
using Nakama;
using Nakama.TinyJson;
public partial class Chat_Box : Control
{
	private ClientNode ClientNode => this.Autoload<ClientNode>();
	private IMatch match;
	public void SetMatch(IMatch X) => match = X;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() { }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) { }
	public async void _on_button_pressed()
	{
		var message = GetNode<LineEdit>("TextBox").Text;
		GetNode<ItemList>("ChatFrame").AddItem($"Me: {message}");
		GetNode<LineEdit>("TextBox").Clear();
		var opCode = 6;
		await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(message));
	}
}







