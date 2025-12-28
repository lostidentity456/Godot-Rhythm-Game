using Godot;
using System;
using System.Collections.Generic;

public partial class Snake : Area2D
{
    [Export] public float Speed = 300.0f;
    [Export] public float Frequency = 5.0f;
    [Export] public float Amplitude = 100.0f;
    [Export] public int Damage = 1;
    [Export] public int TrailLength = 150;

    // Visuals
    private Line2D _trail;
    private Queue<Vector2> _pointHistory = new();

    // Collision Pool
    private List<CollisionShape2D> _colliders = new();
    private int _collisionResolution = 5; // Add collision every 5 points

    private float _timeAlive = 0.0f;
    private float _startX;

    public override void _Ready()
    {
        _startX = GlobalPosition.X;
        _trail = GetNode<Line2D>("Line2D");
        _trail.TopLevel = true; // Trail moves independently in world space

        BodyEntered += OnBodyEntered;

        // Initialize Collision Pool
        // We need (TrailLength / Resolution) shapes.
        int shapeCount = TrailLength / _collisionResolution;

        for (int i = 0; i < shapeCount; i++)
        {
            var shape = new CollisionShape2D();
            var circle = new CircleShape2D();
            circle.Radius = 15.0f; // Size of the hazard
            shape.Shape = circle;

            AddChild(shape); // Add to Snake Area2D
            _colliders.Add(shape);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _timeAlive += dt;

        // 1. Move Head
        float newY = GlobalPosition.Y - (Speed * dt);
        float waveOffset = Mathf.Sin(_timeAlive * Frequency) * Amplitude;
        float newX = _startX + waveOffset;
        GlobalPosition = new Vector2(newX, newY);

        // 2. Update Trail History
        _pointHistory.Enqueue(GlobalPosition);
        if (_pointHistory.Count > TrailLength)
        {
            _pointHistory.Dequeue();
        }

        Vector2[] points = _pointHistory.ToArray();
        _trail.Points = points;

        int colliderIndex = 0;

        // Start from end of array (Head) and go backwards
        for (int i = points.Length - 1; i >= 0; i -= _collisionResolution)
        {
            if (colliderIndex >= _colliders.Count) break;

            var collider = _colliders[colliderIndex];
            collider.GlobalPosition = points[i];

            float scale = (float)i / points.Length;
            collider.Scale = new Vector2(scale, scale);

            colliderIndex++;
        }
        if (_pointHistory.Count > 0)
        {
            // Peek() gets the object at the START of the queue (The Tail / Oldest point)
            Vector2 tailPosition = _pointHistory.Peek();

            // 2. Check if the Tail is off-screen (Top)
            // Screen Top is Y=0. Let's use -100 as a safety margin.
            if (tailPosition.Y < -100)
            {
                QueueFree();
            }
        }
    }

    public override void _ExitTree()
    {
        if (_trail != null) _trail.QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player) player.TakeDamage(Damage);
    }
}