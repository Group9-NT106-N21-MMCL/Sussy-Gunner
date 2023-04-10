using Godot;
using System;

public partial class bullet : CharacterBody2D
{
    public override void _Process(double delta)
    {
        double speed = 20;
        double moveAmount = speed * delta;
        
    }
}
