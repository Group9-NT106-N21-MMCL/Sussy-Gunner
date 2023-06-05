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
    private bool isFlip = false;
    private bool SendIdleState = false;
    private int ammoAmount = 20;
    private int health = 10;
    private int DeathCountDown = 500;
    AnimationPlayer animationPlayer;
    Sprite2D body, DeathBody, gun;
    Marker2D bulletPos;
    CollisionShape2D colision, PlayerArea;
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
    }
    public override void _PhysicsProcess(double delta)
    {
        if (Name == ClientNode.Session.Username)
        {
            var CheckChangePos = Position;
            if (health <= 0) //User dead
            {
                if (!DeathBody.Visible) //Still not send match state
                {
                    var opCode = 3; //Send state dead
                    Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson("Dead!")));

                    body.Visible = gun.Visible = false;
                    DeathBody.Visible = colision.Disabled = PlayerArea.Disabled = true;
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
                    LetLive();
                }
            }
            else
            {
                gun.LookAt(GetGlobalMousePosition());
                GetParent().GetNode<Camera2D>("Camera2D").Position = Position;

                var inputDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
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
        if (Name == ClientNode.Session.Username)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && ammoAmount > 0)
                {
                    Task.Run(async () => await Shoot());

                    var opCode = 2;
                    var state = new ClientNode.PlayerState { GunRoate = gun.Rotation, GunFlip = gun.FlipV };
                    Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(state)));
                }
            }
        }
    }
    public async Task Shoot(float? Rotation = null)
    {
        if (health <= 0) return; //User dead cannot shoot
        var bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
        var _bullet = bulletScene.Instantiate<bullet>();

        _bullet.Rotation = (float)((Rotation == null) ? gun.Rotation : Rotation);
        _bullet.Position = bulletPos.GlobalPosition;
        _bullet.Scale = new Vector2((float)0.5, (float)0.5);
        _bullet.SetMatch(match);
        ammoAmount -= 1;

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
    private void _on_area_area_entered(Area2D area)
    {
        if (area.Name == "BulletArea")
            health -= 2;
    }
}


