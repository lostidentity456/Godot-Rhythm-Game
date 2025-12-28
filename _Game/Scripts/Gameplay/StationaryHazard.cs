using Godot;
using System;

public partial class StationaryHazard : Area2D
{
    [Export] public float WarningTime = 1.0f;
    [Export] public float ActiveTime = 1.0f;
    [Export] public int Damage = 1;

    public bool ShootBullets = false; 
    public PackedScene BulletRef;     // Level script passes the bullet scene here
    public Vector2 ShootDirection;    // Where to aim

    private CollisionShape2D _collision;
    private CanvasItem _visual;

    public override void _Ready()
    {
        _collision = GetNode<CollisionShape2D>("CollisionShape2D");
        foreach (Node child in GetChildren())
        {
            if (child is CanvasItem item && child is not CollisionShape2D)
            {
                _visual = item;
                break;
            }
        }

        BodyEntered += OnBodyEntered;
        StartSequence();
    }

    private void StartSequence()
    {
        _collision.SetDeferred("disabled", true);
        _visual.Modulate = new Color(1, 1, 1, 0.2f);

        Tween tween = CreateTween();
        tween.TweenInterval(WarningTime);
        tween.TweenCallback(Callable.From(Activate));
        tween.TweenProperty(_visual, "modulate", Color.FromHtml("#ff2170"), 0.05f);
        tween.TweenInterval(ActiveTime);
        tween.TweenCallback(Callable.From(() => _collision.SetDeferred("disabled", true)));
        tween.TweenProperty(_visual, "modulate:a", 0.0f, 0.5f);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    private void Activate()
    {
        _collision.SetDeferred("disabled", false);

        // NEW: Fire Bullets if enabled
        if (ShootBullets && BulletRef != null)
        {
            FireCone();
        }
    }

    private void FireCone()
    {
        int bulletCount = 5;
        float spreadAngle = Mathf.DegToRad(45);
        float startAngle = -spreadAngle / 2;
        float angleStep = spreadAngle / (bulletCount - 1);

        Vector2 edgeOffset = ShootDirection * 60.0f;

        for (int i = 0; i < bulletCount; i++)
        {
            var bullet = (Bullet)BulletRef.Instantiate();

            bullet.GlobalPosition = GlobalPosition + edgeOffset;

            float currentOffset = startAngle + (angleStep * i);
            Vector2 finalDir = ShootDirection.Rotated(currentOffset);

            bullet.Direction = finalDir;
            bullet.Speed = 400.0f;

            GetParent().AddChild(bullet);
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player) player.TakeDamage(Damage);
    }
}