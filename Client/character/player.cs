using Godot;
using System;
using System.Threading.Tasks;
using Chickensoft.GoDotNet;
using Nakama;
using Nakama.TinyJson;
using System.Threading;

public partial class player : CharacterBody2D
{
	private ClientNode ClientNode => this.Autoload<ClientNode>();
	private IMatch match;
	public void SetMatch(IMatch X) => match = X;
	public const float Speed = 300.0f;
	private bool isFlip = false;
	private int ammoAmount = 20;
	private int health = 10;
	private int DeathCountDown = 500;
	public void DecHealth() => health -= 2;
	public void SetHealth(int X) => health = X;
	private void LetMove() => MoveAndSlide();
	private void LetDead() => GetNode<AnimationPlayer>("Character/Animation").Play("death");
	private void LetStop() => GetNode<AnimationPlayer>("Character/Animation").Play("idle");
	public override void _PhysicsProcess(double delta)
	{
		if (Name == ClientNode.Session.Username)
		{
			var animationPlayer = GetNode<AnimationPlayer>("Character/Animation");
			var body = GetNode<Sprite2D>("Character/Body");
			var DeathBody = GetNode<Sprite2D>("Character/DeathBody");
			var gun = GetNode<Sprite2D>("Character/Hand");
			var colision = GetNode<CollisionShape2D>("Collision");
			var areashape = GetNode<CollisionShape2D>($"Player_{ClientNode.Session.UserId}/AreaShape");

			if (health <= 0)
			{
				if (!DeathBody.Visible)
				{
					var opCode = 3; //Send live or dead
					Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson("Dead!")));
					body.Visible = gun.Visible = false;
					DeathBody.Visible = colision.Disabled = areashape.Disabled = true;
					DeathBody.FlipH = body.FlipH;
					animationPlayer.Play("death");
				}
				--DeathCountDown;
				if (DeathCountDown <= 0)
				{
					var opCode = 3;
					Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson("Live!")));
					health = 10;
					DeathCountDown = 500;

					animationPlayer.Play("idle");
					body.Visible = gun.Visible = true;
					DeathBody.Visible = colision.Disabled = areashape.Disabled = false;
				}
			}
			else
			{
				gun.LookAt(GetGlobalMousePosition());
				var inputDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

				if (Input.IsActionPressed("reload") && ammoAmount == 0)
					ammoAmount = 20; //Reload bullet
				if (inputDirection.X != 0)
					body.FlipH = inputDirection.X < 0;

				gun.FlipV = body.FlipH;
				var state = new ClientNode.PlayerState { isDirection = true, PosX = inputDirection.X, PosY = inputDirection.Y, GunRoate = gun.Rotation, GunFlip = gun.FlipV };
				var opCode = 0; //Send position
				Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state)));

				if (inputDirection != Vector2.Zero)
					animationPlayer.Play("running");
				else animationPlayer.Play("idle");

				Velocity = inputDirection * Speed;
				GetParent().GetNode<Camera2D>("Camera2D").Position = Position;
				MoveAndSlide();
			}
		}
	}
	public async Task Shoot()
	{
		if (health <= 0) return;
		var bulletPos = GetNode<Marker2D>("Character/Hand/BulletPos");
		var bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		var _bullet = bulletScene.Instantiate<bullet>();
		var gun = GetNode<Sprite2D>("Character/Hand");

		_bullet.Rotation = gun.Rotation;
		_bullet.Position = bulletPos.GlobalPosition;
		_bullet.Scale = new Vector2((float)0.5, (float)0.5);
		_bullet.SetMatch(match);
		ammoAmount -= 1;
		GetParent().AddChild(_bullet);
	}
	public async Task Move(Vector2 Direction, float GunRoate, bool GunFlip, string UserID)
	{
		if (health <= 0) return;
		var animationPlayer = GetNode<AnimationPlayer>("Character/Animation");
		var body = GetNode<Sprite2D>("Character/Body");
		var DeathBody = GetNode<Sprite2D>("Character/DeathBody");
		var gun = GetNode<Sprite2D>("Character/Hand");
		var colision = GetNode<CollisionShape2D>("Collision");
		var areashape = GetNode<CollisionShape2D>($"Player_{UserID}/AreaShape");

		body.Visible = gun.Visible = true;
		DeathBody.Visible = colision.Disabled = areashape.Disabled = false;

		if (Direction != Vector2.Zero)
			animationPlayer.Play("running");
		else animationPlayer.Play("idle");

		Velocity = (Vector2)(Direction * Speed);
		gun.Rotation = GunRoate;
		gun.FlipV = body.FlipH = GunFlip;

		CallDeferred("LetMove");
	}
	public async Task Stop() => CallDeferred("LetStop");
	public async Task DeadOrLive(string UserID, bool Live = false)
	{
		var animationPlayer = GetNode<AnimationPlayer>("Character/Animation");
		var body = GetNode<Sprite2D>("Character/Body");
		var DeathBody = GetNode<Sprite2D>("Character/DeathBody");
		var gun = GetNode<Sprite2D>("Character/Hand");
		var colision = GetNode<CollisionShape2D>("Collision");
		var areashape = GetNode<CollisionShape2D>($"Player_{UserID}/AreaShape");

		body.Visible = gun.Visible = Live;
		DeathBody.Visible = colision.Disabled = areashape.Disabled = !Live;

		DeathBody.FlipH = body.FlipH;
		if (!Live) CallDeferred("LetDead");
		else CallDeferred("LetStop");
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (Name == ClientNode.Session.Username)
		{
			if (@event is InputEventMouseButton mouseEvent)
			{
				if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && ammoAmount > 0)
				{
					Task.Run(async () => await Shoot());
					var opCode = 2;
					Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, "Shoot!"));
				}
			}
		}
	}
}
