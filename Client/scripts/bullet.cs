using Godot;
using System;

public partial class bullet : CharacterBody2D
{
    float maxRange = 1000;
    float distanceTravelled = 0;
    public float damage = 20;
    float speed = 800;
    public override void _Process(double delta)
    {
        float moveAmount = (float)(speed * delta);
        Position += Transform.X.Normalized() * (float)moveAmount;
        distanceTravelled += moveAmount;
        if(distanceTravelled > maxRange)
        {
            QueueFree();
        }
    }

}
