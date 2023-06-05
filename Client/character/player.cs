using Godot;
using System;
using System.Threading.Tasks;
using Chickensoft.GoDotNet;
using Nakama;
using Nakama.TinyJson;

public partial class player : CharacterBody2D
{
	private ClientNode ClientNode => this.Autoload<ClientNode>();
	private IMatch match;
	public const float Speed = 300.0f;
	private bool isFlip = false, SendIdleState = false;
	private int ammoAmount = 20, health = 10, DeathCountDown = 500, Kill = 0, Dead = 0;
	AnimationPlayer animationPlayer;
	Sprite2D body, DeathBody, gun;
	Node2D HealthBar;
	Marker2D bulletPos;
	PackedScene bulletScene;
	CollisionShape2D colision, PlayerArea;
	public void IncDead() => ++Dead;
	public void IncKill() => ++Kill;
	public int GetKill() => Kill;
	public int GetDead() => Dead;
	public void SetHealth(int X) => health = X;
	public void SetMatch(IMatch X) => match = X;
	public void SetGunRotate(float X) => gun.Rotation = X;
	public void SetFlip(bool Flip) => gun.FlipV = body.FlipH = Flip;
	private void LetDead() => GetNode<AnimationPlayer>("Character/Animation").Play("death");
	private void LetLive() => GetNode<AnimationPlayer>("Character/Animation").Play("idle");
	private void LetMove()
	{
		GetNode<AnimationPlayer>("Character/Animation").Play("running");
		MoveAndSlide();
	}
	private void Add_Child(bullet X) => GetParent().AddChild(X);

	public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("Character/Animation");
		body = GetNode<Sprite2D>("Character/Body");
		DeathBody = GetNode<Sprite2D>("Character/DeathBody");
		gun = GetNode<Sprite2D>("Character/Hand");
		colision = GetNode<CollisionShape2D>("Collision");
		bulletPos = GetNode<Marker2D>("Character/Hand/BulletPos");
		PlayerArea = GetNode<CollisionShape2D>("Area/AreaShape");
		HealthBar = GetNode<Node2D>("HealthBar");
		bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
	}
	public override void _PhysicsProcess(double delta)
	{
		if (Name == ClientNode.Session.UserId)
		{
			if (health <= 0) //User dead
			{
				if (!DeathBody.Visible) //Still not send match state
				{
					var opCode = 3; //Send state dead
					Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson("Dead!")));

					body.Visible = gun.Visible = false;
					DeathBody.Visible = colision.Disabled = PlayerArea.Disabled = true;
					++Dead;
					LetDead();
				}
				--DeathCountDown;
				if (DeathCountDown <= 0)
				{
					var opCode = 3; //Send state alive
					Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson("Live!")));

					health = 10;
					DeathCountDown = 500;
					body.Visible = gun.Visible = true;
					DeathBody.Visible = colision.Disabled = PlayerArea.Disabled = false;
					foreach (Sprite2D heart in HealthBar.GetChildren())
						heart.Visible = !heart.Visible;
					LetLive();
				}
			}
			else
			{
				gun.LookAt(GetGlobalMousePosition());
				GetParent().GetNode<Camera2D>("Camera2D").Position = Position;

				var inputDirection = new Vector2(0, 0);
				if (!GetParent().GetNode<Control>("Quit/ChatButton/Chat_Box").Visible)
					inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
				if (Input.IsActionPressed("reload") && ammoAmount == 0)
				{
					ammoAmount = 20; //Reload bullet
					GetParent().GetNode<Label>("RemainingBullet/BulletNumber").Text = $"{ammoAmount}/20";
				}

				if (inputDirection.X != 0)
					body.FlipH = inputDirection.X < 0;
				gun.FlipV = body.FlipH;

				if (inputDirection != Vector2.Zero)
				{
					SendIdleState = false;
					Velocity = inputDirection * Speed;
					LetMove();
					var opCode = 0; //Send position
					var state = new ClientNode.PlayerState { isDirection = true, PosX = inputDirection.X, PosY = inputDirection.Y, GunRoate = gun.Rotation, GunFlip = gun.FlipV };
					Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state)));
				}
				else
				{
					LetLive();
					if (!SendIdleState)
					{
						SendIdleState = true;
						var opCode = 0; //Send position
						var state = new ClientNode.PlayerState { isDirection = false, PosX = Position.X, PosY = Position.Y, GunRoate = gun.Rotation, GunFlip = gun.FlipV };
						Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state)));
					}
				}
			}
		}
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (Name == ClientNode.Session.UserId)
		{
			if (@event is InputEventMouseButton mouseEvent)
			{
				if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && ammoAmount > 0)
				{
					Task.Run(async () => await Shoot(ClientNode.Session.UserId));

					var opCode = 2;
					var state = new ClientNode.PlayerState { GunRoate = gun.Rotation, GunFlip = gun.FlipV };
					Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state)));
				}
			}
		}
	}
	public async Task Shoot(string UserID, float? Rotation = null)
	{
		if (health <= 0) return; //User dead cannot shoot
		var _bullet = bulletScene.Instantiate<bullet>();

		_bullet.GetNode<Area2D>("BulletArea").Name = $"BulletArea_{UserID}";
		_bullet.Rotation = (float)((Rotation == null) ? gun.Rotation : Rotation);
		_bullet.Position = bulletPos.GlobalPosition;
		_bullet.Scale = new Vector2((float)0.5, (float)0.5);
		ammoAmount -= 1;
		GetParent().GetNode<Label>("RemainingBullet/BulletNumber").Text = $"{ammoAmount}/20";

		CallDeferred("Add_Child", _bullet);
	}
	public async Task Move(Vector2 Direction, float GunRoate, bool GunFlip)
	{
		if (health <= 0) return;

		body.Visible = gun.Visible = true;
		DeathBody.Visible = colision.Disabled = PlayerArea.Disabled = false;

		Velocity = (Vector2)(Direction * Speed);
		gun.Rotation = GunRoate;
		gun.FlipV = body.FlipH = GunFlip;
		CallDeferred("LetMove");
	}
	public async Task Stop() => CallDeferred("LetLive");
	public async Task DeadOrLive(bool Live = false)
	{
		body.Visible = gun.Visible = Live;
		DeathBody.Visible = colision.Disabled = PlayerArea.Disabled = !Live;
		DeathBody.FlipH = body.FlipH;

		if (Live)
		{
			CallDeferred("LetLive");
			foreach (Sprite2D heart in HealthBar.GetChildren())
				heart.Visible = !heart.Visible;
		}
		else CallDeferred("LetDead");
	}
	private void _on_area_area_entered(Area2D area)
	{
		var AreaName = area.Name.ToString();
		if (AreaName.StartsWith("BulletArea"))
		{
			var IDWhoShoot = AreaName.Split('_')[1];
			if (IDWhoShoot == Name) return;

			health -= 2;
			foreach (Sprite2D heart in HealthBar.GetChildren())
				if (heart.Visible)
				{
					heart.Visible = false;
					break;
				}
			if (health == 0)
			{
				var opCode = 7;
				Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(IDWhoShoot)));
			}
		}
	}
}


