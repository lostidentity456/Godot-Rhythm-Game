using Godot;
using System;

public partial class VisualizerBar : ColorRect
{
    [Export] public int Damage = 1;

    private Area2D _area;
    private CollisionShape2D _collider;
    private RectangleShape2D _shape;

    public override void _Ready()
    {
        _area = GetNode<Area2D>("Area2D");
        _collider = GetNode<CollisionShape2D>("Area2D/CollisionShape2D");

        // Ensure shape is unique so bars don't share the same size
        _shape = (RectangleShape2D)_collider.Shape.Duplicate();
        _collider.Shape = _shape;

        _area.BodyEntered += OnBodyEntered;
    }

    // Run every frame to sync Physics with Visuals
    public override void _Process(double delta)
    {
        // Get the current size (which changes via Tween)
        Vector2 currentSize = Size;

        // 1. Resize Hitbox
        _shape.Size = currentSize;

        // 2. Reposition Hitbox
        // ColorRect draws from Top-Left (0,0).
        // CollisionShape draws from Center.
        // So we move the collider to (Width/2, Height/2).
        _collider.Position = currentSize / 2.0f;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!Visible) return;

        if (GetParent() is CanvasItem parent && parent.Modulate.A <= 0.01f)
        {
            return;
        }
        if (body is Player player)
        {
            player.TakeDamage(Damage);
        }
    }
}