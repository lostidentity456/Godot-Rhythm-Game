using Godot;
using System;

public partial class Bullet : Area2D
{
    [Export] public int Damage = 1;
    [Export] public float Speed = 300.0f;
    [Export] public Vector2 Direction = Vector2.Left;

    [Export] public float Gravity = 0.0f;
    private Vector2 _currentVelocity;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        GetTree().CreateTimer(10.0f).Timeout += QueueFree;
        _currentVelocity = Direction * Speed;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // Apply Gravity if it exists
        if (Gravity > 0)
        {
            _currentVelocity.Y += Gravity * dt;
        }

        // Move
        GlobalPosition += _currentVelocity * dt;

        // Optional: Rotate bullet to face travel direction (great for rain)
        if (Gravity > 0)
        {
            Rotation = _currentVelocity.Angle();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        // Check if the body is actually the Player
        if (body is Player player)
        {
            player.TakeDamage(Damage);
            QueueFree();
        }
    }
}