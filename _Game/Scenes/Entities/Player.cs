using Godot;

public partial class Player : CharacterBody2D
{
    [ExportCategory("Movement")]
    [Export] public float Speed = 400.0f;

    [ExportCategory("Visuals")]
    [Export] public Vector2 PlayerSize = new Vector2(0.033f, 0.033f);

    [ExportCategory("Dash")]
    [Export] public float DashSpeed = 800.0f;     // How fast player go
    [Export] public float DashDuration = 0.2f;     // How long the dash lasts (in seconds)
    [Export] public float DashCooldown = 1.0f;     // How long until player can dash again

    [ExportCategory("Stats")]
    [Export] public int MaxHealth = 3;
    private int _currentHealth;

    // Safety timer
    [Export] public float MercyInvincibilityTime = 1.0f;
    private bool _isMercyInvincible = false;

    // State Variables
    private bool _isDashing = false;
    private bool _canDash = true;

    private Sprite2D _mainSprite;
    private Sprite2D _ghostX;
    private Sprite2D _ghostY;
    private Sprite2D _ghostD;

    private float _screenWidth;
    private float _screenHeight;

    public override void _Ready()
    {
        Scale = PlayerSize;
        _currentHealth = MaxHealth;

        _mainSprite = GetNode<Sprite2D>("Sprite2D");
        _ghostX = GetNode<Sprite2D>("GhostX");
        _ghostY = GetNode<Sprite2D>("GhostY");
        _ghostD = GetNode<Sprite2D>("GhostD");

        Rect2 viewport = GetViewportRect();
        _screenWidth = viewport.Size.X;
        _screenHeight = viewport.Size.Y;
    }

    public override void _PhysicsProcess(double delta)
    {
        // If player is dashing, ignore normal controls
        if (_isDashing)
        {
            MoveAndSlide();
            return;
        }

        // Standard Movement Input
        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        if (direction != Vector2.Zero)
        {
            Velocity = direction * Speed;
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        // Check for Dash Input
        // Player only dash if moving and cooldown is ready
        if (Input.IsActionJustPressed("dash") && _canDash && direction != Vector2.Zero)
        {
            StartDash(direction);
        }

        MoveAndSlide();
        HandleScreenWrapping();
    }

    private void HandleScreenWrapping()
    {
        Vector2 pos = GlobalPosition;
        float margin = 40.0f; // Approx half player size

        // --- 1. PHYSICS TELEPORT (Actual Body) ---
        bool teleported = false;

        // Horizontal
        if (pos.X > _screenWidth + margin) { pos.X = -margin; teleported = true; }
        else if (pos.X < -margin) { pos.X = _screenWidth + margin; teleported = true; }

        // Vertical
        if (pos.Y > _screenHeight + margin) { pos.Y = -margin; teleported = true; }
        else if (pos.Y < -margin) { pos.Y = _screenHeight + margin; teleported = true; }

        if (teleported) GlobalPosition = pos;

        // --- 2. VISUAL GHOSTS (Seamless Effect) ---

        // Reset all ghosts
        _ghostX.Visible = false;
        _ghostY.Visible = false;
        _ghostD.Visible = false;

        // Sync Animations/Color
        SyncGhost(_ghostX);
        SyncGhost(_ghostY);
        SyncGhost(_ghostD);

        // Calculate Offsets
        // 0 = No wrap, 1 = Right->Left, -1 = Left->Right
        float wrapDirX = 0;
        float wrapDirY = 0;

        // Check Horizontal Edge
        if (pos.X > _screenWidth - margin) wrapDirX = -1; // Show on Left
        else if (pos.X < margin) wrapDirX = 1;  // Show on Right

        // Check Vertical Edge
        if (pos.Y > _screenHeight - margin) wrapDirY = -1; // Show on Top
        else if (pos.Y < margin) wrapDirY = 1;  // Show on Bottom

        // Apply Positions based on wrap direction
        if (wrapDirX != 0)
        {
            _ghostX.Visible = true;
            _ghostX.Position = new Vector2(_screenWidth * wrapDirX, 0);
        }

        if (wrapDirY != 0)
        {
            _ghostY.Visible = true;
            _ghostY.Position = new Vector2(0, _screenHeight * wrapDirY);
        }

        // Corner Case (Diagonal)
        // If we are wrapping BOTH X and Y, we need the diagonal ghost
        if (wrapDirX != 0 && wrapDirY != 0)
        {
            _ghostD.Visible = true;
            _ghostD.Position = new Vector2(_screenWidth * wrapDirX, _screenHeight * wrapDirY);
        }
    }

    private void SyncGhost(Sprite2D ghost)
    {
        ghost.Scale = _mainSprite.Scale;
        ghost.Modulate = _mainSprite.Modulate;
        ghost.Rotation = _mainSprite.Rotation;
    }


    private async void StartDash(Vector2 direction)
    {
        _isDashing = true;
        _canDash = false;

        Tween tween = CreateTween();

        // Instantly turn semi-transparent 
        Modulate = new Color(0, 1, 1, 0.5f); 

        // Stretch length-wise (based on direction)
        tween.TweenProperty(this, "scale", PlayerSize * new Vector2(1.2f, 0.8f), 0.05f);

        Velocity = direction * DashSpeed;

        // Wait for Dash Duration
        await ToSignal(GetTree().CreateTimer(DashDuration), SceneTreeTimer.SignalName.Timeout);

        // --- END DASH ---
        _isDashing = false;
        Velocity = Vector2.Zero;

        // Return to Normal Visuals
        Tween recoveryTween = CreateTween();
        recoveryTween.SetParallel(true); 

        // Fade back to Opaque White
        recoveryTween.TweenProperty(this, "modulate", Colors.White, 0.1f);
        // Snap back to original size
        recoveryTween.TweenProperty(this, "scale", PlayerSize, 0.1f);

        await ToSignal(GetTree().CreateTimer(DashCooldown), SceneTreeTimer.SignalName.Timeout);
        _canDash = true;
    }

    public void TakeDamage(int amount)
    {
        // If player is dashing OR is recently hit, ignore damage
        if (_isDashing || _isMercyInvincible) return;

        // Apply Damage
        _currentHealth -= amount;
        GD.Print($"Ouch! HP: {_currentHealth}");

        // Check Death
        if (_currentHealth <= 0)
        {
            Die();
            return;
        }

        // Trigger "Mercy" Invincibility
        StartMercyFrames();
    }

    private async void StartMercyFrames()
    {
        _isMercyInvincible = true;

        // Visual Feedback: Flash Red/White quickly
        // We can use a loop to blink the character
        Tween blinkTween = CreateTween();
        blinkTween.SetLoops(5); // Blink 5 times
        blinkTween.TweenProperty(this, "modulate:a", 0.2f, 0.1f); // Fade out
        blinkTween.TweenProperty(this, "modulate:a", 1.0f, 0.1f); // Fade in

        await ToSignal(GetTree().CreateTimer(MercyInvincibilityTime), SceneTreeTimer.SignalName.Timeout);

        _isMercyInvincible = false;
        Modulate = Colors.White; // Reset color just in case
    }

    private void Die()
    {
        GD.Print("GAME OVER");
        // For now, just reload the scene so we can keep testing
        GetTree().ReloadCurrentScene();
    }
}