using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chickensoft.GoDotNet;
using Nakama;
using Nakama.TinyJson;

public partial class player : CharacterBody2D
{
	private Queue<int> Q = new Queue<int>();
	private double BuffTime = 0.0;
	public bool isHost = false;
	private ClientNode ClientNode => this.Autoload<ClientNode>();
	private IMatch match;
	private bool isThreeBullet = false, isFireBullet = false, isIceBullet = false;
	private float Speed = 300.0f;
	private bool isFlip = false, SendIdleState = false;
	private int ammoAmount = 20, health = 10, DeathCountDown = 500, Kill = 0, Dead = 0;
	AnimationPlayer animationPlayer;
	Sprite2D body, DeathBody, gun, heart1, heart2, heart3, heart4, heart5;
	Node2D HealthBar;
	Marker2D bulletPos;
	PackedScene NormalBullet, FireBullet, IceBullet;
	CollisionShape2D colision, PlayerArea;
	public void IncDead() => ++Dead;
	public void IncKill() => ++Kill;
	public int GetAmmor() => ammoAmount;
	public int GetKill() => Kill;
	public int GetDead() => Dead;
	public void SetHealth(int X) => health = X;
	public void SetMatch(IMatch X) => match = X;
	public void SetGunRotate(float X) => gun.Rotation = X;
	public void SetFlip(bool Flip) => gun.FlipV = body.FlipH = Flip;
	public void SetFireBullet(bool _isFireBullet) => isFireBullet = _isFireBullet;
	public void SetIceBullet(bool _isIceBullet) => isFireBullet = _isIceBullet;
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
		PlayerArea = GetNode<CollisionShape2D>("PlayerArea/AreaShape");
		HealthBar = GetNode<Node2D>("HealthBar");
		NormalBullet = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		FireBullet = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		IceBullet = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		heart1 = GetNode<Sprite2D>("HealthBar/Health1");
		heart2 = GetNode<Sprite2D>("HealthBar/Health2");
		heart3 = GetNode<Sprite2D>("HealthBar/Health3");
		heart4 = GetNode<Sprite2D>("HealthBar/Health4");
		heart5 = GetNode<Sprite2D>("HealthBar/Health5");
	}
	public async override void _PhysicsProcess(double delta)
	{
		BuffTime += delta;
		if (BuffTime >= 10)
		{
			BuffTime = 0.0;
			Speed = 300.0f;
			if (Q.Count != 0)
			{
				switch (Q.Dequeue())
				{
					case 0:
						isFireBullet = false;
						break;
					case 1:
						isIceBullet = false;
						break;
					case 2:
						isThreeBullet = false;
						break;
					case 3:
						Scale = new Vector2((float)0.5, (float)0.5);
						break;
				}
			}
		}
		if (Name == ClientNode.Session.UserId)
		{
			if (health <= 0) //User dead
			{
				if (!DeathBody.Visible) //Still not send match state
				{
					var opCode = 3; //Send state dead
					await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson("Dead!"));

					body.Visible = gun.Visible = false;
					DeathBody.Visible = colision.Disabled = PlayerArea.Disabled = true;
					++Dead;
					LetDead();
				}
				--DeathCountDown;
				if (DeathCountDown <= 0)
				{
					var opCode = 3; //Send state alive
					await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson("Live!"));

					health = 10;
					DeathCountDown = 500;
					body.Visible = gun.Visible = true;
					DeathBody.Visible = colision.Disabled = PlayerArea.Disabled = false;
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
					ammoAmount = 20; //Reload bullet

				if (inputDirection.X != 0)
					body.FlipH = inputDirection.X < 0;
				gun.FlipV = body.FlipH;

				if (inputDirection != Vector2.Zero)
				{
					SendIdleState = false;
					Velocity = inputDirection * Speed;
					LetMove();
					var opCode = 0; //Send position
					var state = new ClientNode.PlayerState { isDirection = true, PosX = inputDirection.X, PosY = inputDirection.Y, Health = health, GunRoate = gun.Rotation, GunFlip = gun.FlipV, isFireBullet = isFireBullet, isIceBullet = isIceBullet };
					await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state));
				}
				else
				{
					LetLive();
					if (!SendIdleState)
					{
						SendIdleState = true;
						var opCode = 0; //Send position
						var state = new ClientNode.PlayerState { isDirection = false, PosX = Position.X, PosY = Position.Y, Health = health, GunRoate = gun.Rotation, GunFlip = gun.FlipV, isFireBullet = isFireBullet, isIceBullet = isIceBullet };
						await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state));
					}
				}
			}
		}
	}
	public async Task DisplayHealth()
	{
		switch (health)
		{
			case 10:
				heart1.Visible = heart2.Visible = heart3.Visible = heart4.Visible = heart5.Visible = true;
				break;
			case 8:
				heart1.Visible = heart2.Visible = heart3.Visible = heart4.Visible = true;
				heart5.Visible = false;
				break;
			case 6:
				heart1.Visible = heart2.Visible = heart3.Visible = true;
				heart5.Visible = heart4.Visible = false;
				break;
			case 4:
				heart1.Visible = heart2.Visible = true;
				heart5.Visible = heart4.Visible = heart3.Visible = false;
				break;
			case 2:
				heart1.Visible = true;
				heart5.Visible = heart4.Visible = heart3.Visible = heart2.Visible = false;
				break;
			default:
				heart5.Visible = heart4.Visible = heart3.Visible = heart2.Visible = heart1.Visible = false;
				break;
		}
	}
	public async override void _UnhandledInput(InputEvent @event)
	{
		if (Name == ClientNode.Session.UserId)
		{
			if (@event is InputEventMouseButton mouseEvent)
			{
				if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && ammoAmount > 0)
				{
					await Shoot(ClientNode.Session.UserId);

					var opCode = 2;
					var state = new ClientNode.PlayerState { GunRoate = gun.Rotation, GunFlip = gun.FlipV };
					await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state));
				}
			}
		}
	}
	public async Task Shoot(string UserID, float? Rotation = null)
	{
		if (health <= 0) return; //User dead cannot shoot
		bullet _bullet1, _bullet2, _bullet3;
		if (isFireBullet)
		{
			_bullet1 = FireBullet.Instantiate<bullet>();
			_bullet2 = FireBullet.Instantiate<bullet>();
			_bullet3 = FireBullet.Instantiate<bullet>();
		}
		else if (isIceBullet)
		{
			_bullet1 = IceBullet.Instantiate<bullet>();
			_bullet2 = IceBullet.Instantiate<bullet>();
			_bullet3 = IceBullet.Instantiate<bullet>();
		}
		else
		{
			_bullet1 = NormalBullet.Instantiate<bullet>();
			_bullet2 = NormalBullet.Instantiate<bullet>();
			_bullet3 = NormalBullet.Instantiate<bullet>();
		}

		string type = "Normal";
		if (isIceBullet) type = "Slow";
		else if (isFireBullet) type = "Dame";

		_bullet1.GetNode<Area2D>("BulletArea").Name = $"{type}BulletNum1_{UserID}";
		_bullet2.GetNode<Area2D>("BulletArea").Name = $"{type}BulletNum2_{UserID}";
		_bullet3.GetNode<Area2D>("BulletArea").Name = $"{type}BulletNum3_{UserID}";

		_bullet1.Rotation = (float)((Rotation == null) ? gun.Rotation : Rotation);
		_bullet2.Rotation = _bullet1.Rotation - 0.3f;
		_bullet3.Rotation = _bullet1.Rotation + 0.3f;

		_bullet1.Position = _bullet2.Position = _bullet3.Position = bulletPos.GlobalPosition;
		_bullet1.Scale = _bullet2.Scale = _bullet3.Scale = new Vector2((float)0.5, (float)0.5);

		ammoAmount -= 1;

		if (isThreeBullet)
		{
			CallDeferred("Add_Child", _bullet1);
			CallDeferred("Add_Child", _bullet2);
			CallDeferred("Add_Child", _bullet3);
		}
		else CallDeferred("Add_Child", _bullet1);
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

		if (Live) CallDeferred("LetLive");
		else CallDeferred("LetDead");
	}
	private async void _on_area_area_entered(Area2D area)
	{
		var AreaName = area.Name.ToString();
		if (AreaName.Contains("BulletNum"))
		{
			var IDWhoShoot = AreaName.Split('_')[1];
			if (IDWhoShoot == Name) return;

			if (AreaName.StartsWith("SlowBulletNum"))
			{
				Speed = 150.0f;
				health -= 2;
			}
			else if (AreaName.StartsWith("DameBulletNum"))
				health -= 4;
			else if (AreaName.StartsWith("NormalBulletNum"))
				health -= 2;

			if (health == 0)
			{
				var opCode = 7;
				await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(IDWhoShoot));
			}
		}
		else
		{
			switch (area.Name)
			{
				case "FireBulletArea": //0
					isFireBullet = true;
					Q.Enqueue(0);
					break;
				case "IceBulletArea": //1
					isIceBullet = true;
					Q.Enqueue(1);
					break;
				case "HealingArea":
					health = 10;
					break;
				case "ThreeBulletArea": //2
					isThreeBullet = true;
					Q.Enqueue(2);
					break;
				case "ZoomArea": //3
					Scale = new Vector2((float)0.3, (float)0.3);
					Q.Enqueue(3);
					break;
			}
		}
	}
}


