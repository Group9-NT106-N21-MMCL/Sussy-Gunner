using Godot;
using System;

public partial class main_menu : Control
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
    private void _on_login_button_pressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/login_scene.tscn");
    }
    private void _on_register_button_pressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/register_scene.tscn");
    }

}






