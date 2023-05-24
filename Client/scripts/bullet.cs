using Godot;
using Nakama;
using System;
using System.Threading.Tasks;

public partial class bullet : CharacterBody2D
{
    float maxRange = 1000;
    float distanceTravelled = 0;
    public float damage = 20;
    float speed = 1200;
    private IMatch match;
    public void SetMatch(IMatch X) => match = X;
    public override void _Process(double delta)
    {
        float moveAmount = (float)(speed * delta);
        Position += Transform.X.Normalized() * (float)moveAmount;
        distanceTravelled += moveAmount;
        if (distanceTravelled > maxRange)
            QueueFree();
    }

    public void _on_area_2d_area_entered(Area2D area)
    {
        if (area.Name == "WallArea")
            QueueFree();
        else (area.Name.ToString().StartsWith("Player_"))
        {
            QueueFree();
            var opCode = 1; //Detect who has been shot
            var Username = area.Name.ToString().Substring(7);
            Task.Run(async () => await ClientNode.Socket.SendMatchStateAsync(match.Id, opCode, JsonWriter.ToJson(Username)));
        }
    }
}

