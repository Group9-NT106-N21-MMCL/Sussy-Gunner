using Godot;
using System;

public partial class player : CharacterBody2D
{

	public int Speed = 10;
	private Vector2 _velocity = new Vector2();

	Sprite2D body;
	AnimationPlayer animationPlayer;
	public override void _Ready()
	{
		body = (Sprite2D)GetNode("Texture");
		animationPlayer = (AnimationPlayer)GetNode("RunningAnimation");
	}
	public void GetInput()
	{
		// Detect up/down/left/right keystate and only move when pressed
		_velocity = new Vector2();

		if (Input.IsActionPressed("move_right"))
			_velocity.X += 1;

		if (Input.IsActionPressed("move_left"))
			_velocity.X -= 1;

		if (Input.IsActionPressed("move_down"))
			_velocity.Y += 1;

		if (Input.IsActionPressed("move_up"))
			_velocity.Y -= 1;
		_velocity = _velocity.Normalized() * Speed;
		
		if(_velocity.X > 0){
			body.FlipH = false;
		}
		else if(_velocity.X < 0){
			body.FlipH = true;
		}
		
		if(_velocity.X == 0){
			animationPlayer.Play("idle");
		}
		else {
			animationPlayer.Play("running");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		GetInput();
		MoveAndCollide(_velocity);
	}
}
