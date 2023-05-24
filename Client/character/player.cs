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
    public void SetMatch(IMatch X) => match = X;
    public const float Speed = 300.0f;
    private bool isFlip = false;
    private int ammoAmount = 20;
    public override void _PhysicsProcess(double delta)
    {
        if (Name == ClientNode.Session.Username)
        {
            var animationPlayer = GetNode<AnimationPlayer>("Character/RunningAnimation");
            var body = GetNode<Sprite2D>("Character/Body");
            var gun = GetNode<Sprite2D>("Character/Hand");
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
    public async Task Shoot()
    {
        var bulletPos = GetNode<Marker2D>("Character/Hand/BulletPos");
        var bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
        var _bullet = bulletScene.Instantiate<bullet>();

        _bullet.Rotation = (GetGlobalMousePosition() - GlobalPosition).Angle();
        _bullet.Position = bulletPos.GlobalPosition;
        _bullet.Scale = new Vector2((float)0.5, (float)0.5);
        _bullet.SetMatch(match);
        ammoAmount -= 1;
        GetParent().AddChild(_bullet);
    }
    private void LetMove() => MoveAndSlide();
    public async Task Move(Vector2 Direction, float GunRoate, bool GunFlip)
    {
        var animationPlayer = GetNode<AnimationPlayer>("Character/RunningAnimation");
        var body = GetNode<Sprite2D>("Character/Body");
        var gun = GetNode<Sprite2D>("Character/Hand");

        if (Direction != Vector2.Zero)
            animationPlayer.Play("running");
        else animationPlayer.Play("idle");
        Velocity = (Vector2)(Direction * Speed);
        gun.Rotate(GunRoate);
        gun.FlipV = GunFlip;

        CallDeferred("LetMove");
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (Name == ClientNode.Session.Username)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && ammoAmount > 0)

                    Task.Run(async () => await Shoot());
            }
        }
    }
}
