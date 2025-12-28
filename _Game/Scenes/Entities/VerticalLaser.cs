using Godot;
using System;

public partial class VerticalLaser : Area2D
{
    [Export] public float WarningTime = 1.0f;
    [Export] public float ActiveTime = 1.0f;
    [Export] public float BeamWidth = 100.0f;
    [Export] public int Damage = 1;

    private ColorRect _visual;
    private CollisionShape2D _collision;
    private float _screenHeight;

    public override void _Ready()
    {
        _visual = GetNode<ColorRect>("ColorRect");
        _collision = GetNode<CollisionShape2D>("CollisionShape2D");
        _screenHeight = GetViewportRect().Size.Y;

        BodyEntered += OnBodyEntered;
        StartSequence();
    }

    private void StartSequence()
    {
        // 1. SETUP WARNING (Bottom, 50% Height, Faint)
        _collision.SetDeferred("disabled", true);

        // Visual Setup
        _visual.Size = new Vector2(BeamWidth, _screenHeight * 0.5f);
        _visual.Position = new Vector2(-BeamWidth / 2, -_screenHeight * 0.5f);
        _visual.Modulate = new Color(1, 0, 0, 0.3f); // Faint Red

        Tween tween = CreateTween();

        // 2. WARNING PHASE (Grow to 75%)
        // We use the full warning time to grow from 50% to 75%
        tween.TweenProperty(_visual, "size:y", _screenHeight * 0.75f, WarningTime);
        tween.Parallel().TweenProperty(_visual, "position:y", -_screenHeight * 0.75f, WarningTime);

        // 3. ACTIVATE (Snap to 100% and Solid)
        tween.TweenCallback(Callable.From(Activate));

        // Animate the snap to full height quickly (0.05s)
        tween.TweenProperty(_visual, "size:y", _screenHeight, 0.05f);
        tween.Parallel().TweenProperty(_visual, "position:y", -_screenHeight, 0.05f);
        tween.Parallel().TweenProperty(_visual, "modulate", Color.FromHtml("#ff2170"), 0.05f); // Solid Red

        // 4. ACTIVE DURATION
        tween.TweenInterval(ActiveTime);

        // 5. FADE OUT
        tween.TweenCallback(Callable.From(() => _collision.SetDeferred("disabled", true)));
        tween.TweenProperty(_visual, "modulate:a", 0.0f, 0.3f); // Fade alpha
        tween.TweenCallback(Callable.From(QueueFree));
    }

    private void Activate()
    {
        _collision.SetDeferred("disabled", false);

        // Update Hitbox to full screen height
        var shape = new RectangleShape2D();
        shape.Size = new Vector2(BeamWidth, _screenHeight);
        _collision.Shape = shape;

        // Collision needs to be centered on the beam
        // The beam grows Up from (0,0), so the center is (0, -Height/2)
        _collision.Position = new Vector2(0, -_screenHeight / 2);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player) player.TakeDamage(Damage);
    }
}