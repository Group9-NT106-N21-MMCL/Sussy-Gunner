using Godot;
using System;
using System.Threading.Tasks;
using Chickensoft.GoDotNet;
using Nakama;
using Nakama.TinyJson;

public partial class player : CharacterBody2D
{
    public bool isHost = false;
    private ClientNode ClientNode => this.Autoload<ClientNode>();
    private IMatch match;
    public const float Speed = 300.0f;
    private bool isFlip = false, SendIdleState = false;
    private int ammoAmount = 20, health = 10, DeathCountDown = 500, Kill = 0, Dead = 0;
    AnimationPlayer animationPlayer;
    Sprite2D body, DeathBody, gun, heart1, heart2, heart3, heart4, heart5;
    Node2D HealthBar;
    Marker2D bulletPos;
    PackedScene bulletScene;
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
        heart1 = GetNode<Sprite2D>("HealthBar/Health1");
        heart2 = GetNode<Sprite2D>("HealthBar/Health2");
        heart3 = GetNode<Sprite2D>("HealthBar/Health3");
        heart4 = GetNode<Sprite2D>("HealthBar/Health4");
        heart5 = GetNode<Sprite2D>("HealthBar/Health5");
    }
    public async override void _PhysicsProcess(double delta)
    {
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
                    var state = new ClientNode.PlayerState { isDirection = true, PosX = inputDirection.X, PosY = inputDirection.Y, Health = health, GunRoate = gun.Rotation, GunFlip = gun.FlipV };
                    await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state));
                }
                else
                {
                    LetLive();
                    if (!SendIdleState)
                    {
                        SendIdleState = true;
                        var opCode = 0; //Send position
                        var state = new ClientNode.PlayerState { isDirection = false, PosX = Position.X, PosY = Position.Y, Health = health, GunRoate = gun.Rotation, GunFlip = gun.FlipV };
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

        if (Live) CallDeferred("LetLive");
        else CallDeferred("LetDead");
    }
    private async void _on_area_area_entered(Area2D area)
    {
        var AreaName = area.Name.ToString();
        if (AreaName.StartsWith("BulletArea"))
        {
            var IDWhoShoot = AreaName.Split('_')[1];
            if (IDWhoShoot == Name) return;

            health -= 2;
            if (health == 0)
            {
                var opCode = 7;
                await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(IDWhoShoot));
            }
        }
    }
}


