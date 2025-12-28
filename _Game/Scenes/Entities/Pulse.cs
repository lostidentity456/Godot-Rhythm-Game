using Godot;
using System;

public partial class Pulse : Area2D
{
    [Export] public float ExpandTime = 0.5f; // How fast it grows
    [Export] public float MaxScale = 4.0f;   // How big it gets (4x size)
    [Export] public int Damage = 1;

    public override void _Ready()
    {
        // Start small
        Scale = new Vector2(0.1f, 0.1f);

        BodyEntered += OnBodyEntered;

        // Start the Pulse Animation
        Tween tween = CreateTween();

        // 1. Expand rapidly to Max Scale
        // "TransQuad" and "EaseOut" gives it that explosive "Boom" feel
        tween.TweenProperty(this, "scale", new Vector2(MaxScale, MaxScale), ExpandTime)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        // 2. Fade out while expanding (optional, looks nicer)
        tween.Parallel().TweenProperty(this, "modulate:a", 0.0f, ExpandTime);

        // 3. Delete when done
        tween.TweenCallback(Callable.From(QueueFree));
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            player.TakeDamage(Damage);
        }
    }
}