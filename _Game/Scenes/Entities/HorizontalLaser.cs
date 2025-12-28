using Godot;
using System;

public partial class HorizontalLaser : Area2D
{
    [Export] public bool FromRight = false; // Set this to true for Part 2
    [Export] public float WarningTime = 1.0f;
    [Export] public float ActiveTime = 0.5f;
    [Export] public float BeamHeight = 150.0f;
    [Export] public int Damage = 1;

    private ColorRect _visual;
    private CollisionShape2D _collision;
    private float _screenWidth;

    public override void _Ready()
    {
        _visual = GetNode<ColorRect>("ColorRect");
        _collision = GetNode<CollisionShape2D>("CollisionShape2D");
        _screenWidth = GetViewportRect().Size.X;

        BodyEntered += OnBodyEntered;

        // Force initial state immediately so we don't see a white square
        StartSequence();
    }

    private void StartSequence()
    {
        // 1. INITIAL SETUP
        _collision.SetDeferred("disabled", true);

        // Set Height and vertical centering
        _visual.CustomMinimumSize = new Vector2(0, BeamHeight); // Reset constraints
        _visual.Size = new Vector2(0, BeamHeight); // Start with 0 width

        // Center Y: The root is the spawn point. The rect needs to move up by half height.
        // We will calculate X inside the update loop or tween.
        float yPos = -BeamHeight / 2.0f;

        // Set Color to Warning (Faint Red)
        _visual.Modulate = new Color(1, 0, 0, 0.3f);

        // 2. ANIMATION SEQUENCE
        Tween tween = CreateTween();

        // --- WARNING PHASE ---
        // Step A: Grow to 1/3 (640px) instantly
        // Step B: Grow to 1/2 (960px) over WarningTime

        // We use a custom method to update size so we can handle "FromRight" math easily
        tween.TweenMethod(Callable.From<float>(UpdateBeamWidth), 0.0f, 640.0f, 0.1f);
        tween.TweenMethod(Callable.From<float>(UpdateBeamWidth), 640.0f, 960.0f, WarningTime);

        // --- FIRE PHASE ---
        tween.TweenCallback(Callable.From(Activate));

        // Expand to Full Screen + Solid Red
        tween.Parallel().TweenMethod(Callable.From<float>(UpdateBeamWidth), 960.0f, _screenWidth, 0.05f);
        tween.Parallel().TweenProperty(_visual, "modulate", Color.FromHtml("#ff2170"), 0.05f);

        // --- ACTIVE PHASE ---
        tween.TweenInterval(ActiveTime);

        // --- FADE OUT ---
        tween.TweenCallback(Callable.From(DisableHitbox));
        tween.TweenProperty(_visual, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    // This function handles the direction logic!
    private void UpdateBeamWidth(float width)
    {
        _visual.Size = new Vector2(width, BeamHeight);

        float yPos = -BeamHeight / 2.0f;

        if (FromRight)
        {
            // If coming from Right, the Position must shift Left as it grows.
            // e.g. Width 100 -> Position X = -100
            _visual.Position = new Vector2(-width, yPos);
        }
        else
        {
            // If coming from Left, Position stays at 0
            _visual.Position = new Vector2(0, yPos);
        }
    }

    private void Activate()
    {
        _collision.SetDeferred("disabled", false);
        var shape = new RectangleShape2D();
        shape.Size = new Vector2(_screenWidth, BeamHeight);
        _collision.Shape = shape;

        // Center the collider horizontally relative to the root
        // If FromRight (Root is at 1920), center is at -960
        // If FromLeft (Root is at 0), center is at +960
        float xOffset = FromRight ? -_screenWidth / 2 : _screenWidth / 2;
        _collision.Position = new Vector2(xOffset, 0);
    }

    private void DisableHitbox() { _collision.SetDeferred("disabled", true); }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player) player.TakeDamage(Damage);
    }
}