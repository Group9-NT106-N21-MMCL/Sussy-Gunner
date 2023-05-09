using Godot;
using System;

public partial class player : CharacterBody2D
{
    public int Speed = 7;
    private int ammoAmount = 10;
    private Vector2 _velocity = new Vector2(), _left_offset = new Vector2(-80, 0);
    Sprite2D body, hand;
    Node2D character;
    AnimationPlayer animationPlayer;
    Marker2D marker;
    PackedScene bulletScene;
    Timer timer = new Timer();
    bool isLeft = false;
    public override void _Ready()
    {
        character = (Node2D)GetNode("Character");
        body = (Sprite2D)GetNode("Character/Body");
        hand = (Sprite2D)GetNode("Character/Hand");
        animationPlayer = (AnimationPlayer)GetNode("Character/RunningAnimation");
        bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
        marker = (Marker2D)GetNode("Marker2D");
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

        if (direction.X > 0)
        {
            character.Scale = new Vector2(1, 1);
            //hand.Offset = Vector2.Zero;
            hand.Position = new Vector2(_velocity.X, _velocity.Y);
            isLeft = false;

        }
        // Look at the mouse position
        else if (direction.X < 0)
        {
            character.Scale = new Vector2(-1, 1);
            hand.Position = new Vector2(-_velocity.X, _velocity.Y);
            isLeft = true;
        }

        if (!isMove) animationPlayer.Play("idle");
        else animationPlayer.Play("running");

        if (Input.IsActionPressed("reload") && ammoAmount == 0)
            ammoAmount = 10; //Reload bullet

        if (character.Scale.X < 0)
        {
            hand.Rotation = inv_direction.Angle();
            if (hand.Rotation < -45)
                hand.Rotation = -45;
            else if (hand.Rotation > 45)
                hand.Rotation = 45;
        }
        else
        {
            hand.Rotation = direction.Angle();
            if (hand.Rotation < -45)
                hand.Rotation = -45;
            if (hand.Rotation > 45)
                hand.Rotation = 45;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton eventMouse)
        {
            if (eventMouse.ButtonIndex == MouseButton.Left && eventMouse.Pressed)
            {
                if (ammoAmount > 0) shoot();
            }
        }
    }

    public void shoot()
    {
        Vector2 moveBack = new Vector2((float)40, (float)0);
        var _bullet = (Node2D)bulletScene.Instantiate();
        _bullet.Rotation = (GetGlobalMousePosition() - GlobalPosition).Angle();
        if (!isLeft)
            _bullet.Position = marker.GlobalPosition;
        else
            _bullet.Position = marker.GlobalPosition - moveBack;
        _bullet.Scale = new Vector2((float)0.5, (float)0.5);
        ammoAmount -= 1;
        GetParent().AddChild(_bullet);
    }

    public override void _PhysicsProcess(double delta)
    {
        GetInput();
        MoveAndCollide(_velocity, false, (float)0.8, true);
    }
}
