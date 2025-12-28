using Godot;
using System;

public partial class ThinLaser : Area2D
{
    [Export] public float WarningTime = 1.0f;
    [Export] public float ActiveTime = 0.5f;
    [Export] public float Length = 2000.0f; // Long enough to leave screen
    [Export] public float Thickness = 30.0f;

    private ColorRect _visual;
    private CollisionShape2D _collision;

    public override void _Ready()
    {
        _visual = GetNode<ColorRect>("ColorRect");
        _collision = GetNode<CollisionShape2D>("CollisionShape2D");

        // Align visual so (0,0) is the start of the beam
        _visual.Size = new Vector2(Length, Thickness);
        _visual.Position = new Vector2(0, -Thickness / 2); // Center Y
        _visual.Modulate = new Color(1, 0, 0, 0.3f);

        BodyEntered += (body) => { if (body is Player p) p.TakeDamage(1); };

        StartSequence();
    }

    private void StartSequence()
    {
        _collision.SetDeferred("disabled", true);

        Tween tween = CreateTween();
        // Warning: Just wait
        tween.TweenInterval(WarningTime);

        // Fire
        tween.TweenCallback(Callable.From(Activate));
        tween.TweenProperty(_visual, "modulate", Color.FromHtml("#ff2170"), 0.05f);

        // Active
        tween.TweenInterval(ActiveTime);

        // Fade
        tween.TweenCallback(Callable.From(() => _collision.SetDeferred("disabled", true)));
        tween.TweenProperty(_visual, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    private void Activate()
    {
        _collision.SetDeferred("disabled", false);
        var shape = new RectangleShape2D();
        shape.Size = new Vector2(Length, Thickness);
        _collision.Shape = shape;
        _collision.Position = new Vector2(Length / 2, 0); // Offset center
    }
}