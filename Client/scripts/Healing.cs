using Godot;
using System;

public partial class Healing : Node2D
{
	private void SetDis() => GetNode<CollisionShape2D>("HealingTexture/HealingArea/HealingAreaShape").Disabled = true;
	private void _on_area_2d_area_entered(Area2D area)
	{
		if (area.Name == "PlayerArea")
		{
			Visible = false;
			CallDeferred("SetDis");
		}
	}
}



