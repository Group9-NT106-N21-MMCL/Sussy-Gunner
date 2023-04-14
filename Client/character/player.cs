using Godot;
using System;

public partial class player : CharacterBody2D
{

	public int Speed = 10;
	private Vector2 _velocity = new Vector2();
	private Vector2 _left_offset = new Vector2(-80, 0);

	Sprite2D body;
	Sprite2D hand;
	Node2D character;
	AnimationPlayer animationPlayer;

	PackedScene bulletScene;

	public override void _Ready()
	{
		character = (Node2D)GetNode("Character");
		body = (Sprite2D)GetNode("Character/Body");
		hand = (Sprite2D)GetNode("Character/Hand");
		animationPlayer = (AnimationPlayer)GetNode("Character/RunningAnimation");
		bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
	}
	public void GetInput()
	{
		Vector2 direction = GetGlobalMousePosition();
		direction = (direction - Position).Normalized();
		Vector2 inv_direction = new Vector2(-direction.X, direction.Y).Normalized();
		bool isMove = false;
		// Detect up/down/left/right keystate and only move when pressed
		_velocity = new Vector2();

		if (Input.IsActionPressed("move_right"))
        {
			_velocity.X += 1;
			isMove = true;
		}

		if (Input.IsActionPressed("move_left"))
        {
			_velocity.X -= 1;
			isMove = true;
		}
		if (Input.IsActionPressed("move_down"))
        {
			_velocity.Y += 1;
			isMove = true;
		}
		if (Input.IsActionPressed("move_up"))
        {	
			_velocity.Y -= 1;
			isMove = true;
		}
		_velocity = _velocity.Normalized() * Speed;
		
		if(direction.X > 0){
			character.Scale = new Vector2(1, 1);
			//hand.Offset = Vector2.Zero;
			hand.Position = new Vector2(_velocity.X, _velocity.Y);

		}
		// Look at the mouse position
		else if(direction.X < 0)
		{
			character.Scale = new Vector2(-1,1);
			//hand.Offset = _left_offset;
			hand.Position = new Vector2(-_velocity.X, _velocity.Y);
		}
		
		if(!isMove){
			animationPlayer.Play("idle");
		}
		else {
			animationPlayer.Play("running");
		}


		if (character.Scale.X < 0)
        {
			hand.Rotation = inv_direction.Angle();
			if (hand.Rotation < -45)
			{
				hand.Rotation = -45;
			}
			if (hand.Rotation > 45)
			{
				hand.Rotation = 45;
			}
		}
        else
        {
			hand.Rotation = direction.Angle();
			if (hand.Rotation < -45)
			{
				hand.Rotation = -45;
			}
			if (hand.Rotation > 45)
			{
				hand.Rotation = 45;
			}
		}
	}

    public override void _UnhandledInput(InputEvent @event)
    {
        if(@event is InputEventMouseButton eventMouse)
        {
			if(eventMouse.ButtonIndex == MouseButton.Left && eventMouse.Pressed)
            {
				GD.Print("Shooted");
				shoot();
            }
        }
    }

	public void shoot()
    {
		var _bullet = (Node2D)bulletScene.Instantiate();
		AddChild(_bullet);
		_bullet.Rotation = (GetGlobalMousePosition() - GlobalPosition).Angle();
		_bullet.Position = body.Position;
		AudioStream GunSound = Get

	}

    public override void _PhysicsProcess(double delta)
	{
		GetInput();
		MoveAndCollide(_velocity);
	}
}
