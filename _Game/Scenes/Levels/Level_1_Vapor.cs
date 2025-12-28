using Godot;
using System;
using System.Threading.Tasks;

public partial class Level_1_Vapor : Node2D
{
    [ExportCategory("Resources")]
    [Export] public AudioStream Song;
    [Export] public PackedScene BulletScene;
    [Export] public PackedScene PulseScene;
    [Export] public PackedScene VerticalLaserScene;
    [Export] public PackedScene SnakeScene;
    [Export] public PackedScene HorizontalLaserScene;
    [Export] public PackedScene ThinLaserScene;
    [Export] public PackedScene WarningCircleScene;
    [Export] public PackedScene SideSquareScene;
    [Export] public HBoxContainer VisualizerContainer;

    [ExportCategory("References")]
    [Export] public Camera2D MainCamera;
    [Export] public Label SongNameLabel;
    [Export] public Label AuthorLabel;
    [Export] public ColorRect ScreenFader;

    private Player _playerRef;
    private float _visualizerX;
    private Vector2 _screenCenter;
    private float _beatDuration;


    public override void _Ready()
    {
        GD.Randomize();
        _screenCenter = GetViewportRect().Size / 2;
        MainCamera.Position = _screenCenter;

        Conductor.Instance.Stream = Song;
        Conductor.Instance.Bpm = 155;
        Conductor.Instance.BeatHit += OnBeatHit;

        _beatDuration = 60.0f / 155.0f;
        _playerRef = GetNode<Player>("Player");
        _visualizerX = VisualizerContainer.Position.X;

        VisualizerContainer.Position = new Vector2(_visualizerX, -300);
        VisualizerContainer.Modulate = Colors.White;

        RunIntroSequence();
    }

    private async void RunIntroSequence()
    {
        // 1. SETUP: Black screen, Text Invisible
        ScreenFader.Color = new Color(0, 0, 0, 1);

        // Hide both labels
        SongNameLabel.Modulate = new Color(1, 1, 1, 0);
        AuthorLabel.Modulate = new Color(1, 1, 1, 0);

        // 2. FADE IN (Cinematic)
        Tween introTween = CreateTween();

        // Fade out black screen
        introTween.TweenProperty(ScreenFader, "color:a", 0.0f, 1.5f);

        // Fade in Song Name
        introTween.Parallel().TweenProperty(SongNameLabel, "modulate:a", 1.0f, 1.5f);

        // Fade in Author (Let's make it appear slightly slower/later for style!)
        introTween.Parallel().TweenProperty(AuthorLabel, "modulate:a", 1.0f, 2.0f);

        // Wait...
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);

        // 3. SPAWN PRE-MUSIC HAZARDS
        float warningTime = _beatDuration * 2;
        float activeTime = _beatDuration * 4;
        SpawnSquarePattern(shoot: false);

        // 4. WAIT FOR WARNING
        await ToSignal(GetTree().CreateTimer(warningTime), SceneTreeTimer.SignalName.Timeout);

        // 5. START MUSIC
        Conductor.Instance.PlayWithBeatOffset(0);

        // 6. FADE OUT TEXT
        Tween textFade = CreateTween();
        textFade.TweenProperty(SongNameLabel, "modulate:a", 0.0f, 1.0f);
        textFade.Parallel().TweenProperty(AuthorLabel, "modulate:a", 0.0f, 1.0f);
    }


    private void OnBeatHit(int beat)
    {
        if (beat < 24 && beat % 8 == 6)
        {
            SpawnSquarePattern(shoot: false);
        }

        else if (beat >= 24 && beat < 64 && beat % 8 == 6)
        {
            SpawnSquarePattern(shoot: true);
        }

        else if (beat >= 64 && beat < 124)
        {
            SpawnRainSequence();
            if (beat >= 92 && beat < 124 && beat % 4 == 2)
            {
                SpawnSquarePattern(shoot: false);
            }
        }

        else if (beat >= 126 && beat < 142)
        {
            if (beat == 126)
            {
                SpawnSquarePattern_2(isLeft: true);
            }
            else if (beat == 134)
            {
                SpawnSquarePattern_2(isLeft: false);
            }
            else
            {
                if (beat % 2 == 0)
                {
                    SpawnCircle();
                }
            }
        }

        else if (beat >= 142 && beat < 156)
        {
            SpawnGridSquares(4);
        }
        else if (beat >= 158 && beat < 288)
        {
            Tween slide = CreateTween();
            slide.TweenProperty(VisualizerContainer, "position:y", 20.0f, 0.5f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
            PulseVisualizer();

            if (beat % 2 == 0)
            {
                SpawnSnake();
            }

            if (beat == 158)
            { 
                SpawnVerticalLasers(); 
            }
            else if (beat >= 160 && beat < 192 )
            {
                if (beat % 4 == 0) SpawnHorizontalLaser(fromRight: false);
            }
            else if (beat >= 192 && beat < 224)
            {
                if (beat % 4 == 0) SpawnHorizontalLaser(fromRight: true);
            }
            else if (beat >= 224 && beat < 256)
            {
                if (beat % 4 == 0)
                {
                    bool topRight = (beat % 8 != 0); 
                    SpawnCornerCone(topRight);
                }
            }
            else if (beat >= 256)
            {
                if (beat % 4 == 0)
                {
                    bool topRight = (beat % 8 != 0);
                    SpawnCornerCone(topRight);
                }

                if (beat % 4 == 0)
                {
                    SpawnAimedBullets();
                }
            }
        }
        else if (beat >= 288)
        {
            if (beat == 288)
            {
                Tween slideOut = CreateTween();
                // Move Y back up to -300 (Off-screen)
                // Slower time (2.0s) for a dramatic exit
                slideOut.TweenProperty(VisualizerContainer, "position:y", -300.0f, 2.0f)
                    .SetTrans(Tween.TransitionType.Quad)
                    .SetEase(Tween.EaseType.InOut);
            }
        }
    }

    private void SpawnCircle()
    {
        float warningTime = _beatDuration * 2;
        float activeTime = _beatDuration * 4;

        if (WarningCircleScene != null)
        {
            var circle = (StationaryHazard)WarningCircleScene.Instantiate();
            AddChild(circle);

            float cX = (float)GD.RandRange(400, 1520);
            float cY = (float)GD.RandRange(250, 830);
            circle.GlobalPosition = new Vector2(cX, cY);
            circle.WarningTime = warningTime;
            circle.ActiveTime = activeTime;
        }
    }

    private void SpawnSquarePattern(bool shoot)
    {
        float warningTime = _beatDuration * 2;
        float activeTime = _beatDuration * 4;
        SpawnCircle();

        if (SideSquareScene != null)
        {
            var leftSlots = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4, 5, 6, 7};
            for (int i = 0; i < 2; i++)
            {
                int r = (int)(GD.Randi() % leftSlots.Count);
                int slot = leftSlots[r];
                leftSlots.RemoveAt(r);
                SpawnSingleSquare_1(slot, warningTime, activeTime, shoot);
            }

            var rightSlots = new System.Collections.Generic.List<int> { 8, 9, 10, 11, 12, 13, 14, 15 };
            for (int i = 0; i < 2; i++)
            {
                int r = (int)(GD.Randi() % rightSlots.Count);
                int slot = rightSlots[r];
                rightSlots.RemoveAt(r);
                SpawnSingleSquare_1(slot, warningTime, activeTime, shoot);
            }
        }
    }

    private void SpawnSquarePattern_2(bool isLeft)
    {
        float warningTime = _beatDuration * 2;
        float activeTime = _beatDuration * 4;

        SpawnCircle();

        if (SideSquareScene != null)
        {
            if (isLeft)
            {
                for (int i = 0; i < 8; i++)
                {
                    SpawnSingleSquare_1(i, warningTime, activeTime, shoot: true);
                }
            } 
            else
            {
                for (int i = 8; i < 16; i++)
                {
                    SpawnSingleSquare_1(i, warningTime, activeTime, shoot: true);
                }
            }
        }
    }

    private void SpawnGridSquares(int count)
    {
        if (SideSquareScene == null) return;

        // --- GRID CONFIGURATION ---
        int cols = 13;
        int rows = 8;
        float squareSize = 120f;
        float gapX = 30f;
        float gapY = 15f;
        float marginX = 67.5f; // User defined side margin

        // Calculate total grid height to center it vertically
        // Height = (8 * 120) + (7 * 15) = 1065px
        float totalHeight = (rows * squareSize) + ((rows - 1) * gapY);
        float startY = (1080f - totalHeight) / 2f; // Centers vertically (approx 7.5px top margin)

        // --- SELECT RANDOM SPOTS ---
        // We need 2 distinct coordinates (col, row)
        var selectedSpots = new System.Collections.Generic.List<Vector2I>();

        while (selectedSpots.Count < count)
        {
            int rCol = (int)(GD.Randi() % cols);
            int rRow = (int)(GD.Randi() % rows);
            Vector2I candidate = new Vector2I(rCol, rRow);

            // Ensure distinctness
            if (!selectedSpots.Contains(candidate))
            {
                selectedSpots.Add(candidate);
            }
        }

        // --- SPAWN SQUARES ---
        foreach (Vector2I spot in selectedSpots)
        {
            var square = (StationaryHazard)SideSquareScene.Instantiate();
            AddChild(square);

            // --- POSITION MATH ---
            // X = Margin + (Col * (Size + Gap)) + HalfSize (to center anchor)
            float xPos = marginX + (spot.X * (squareSize + gapX)) + (squareSize / 2);

            // Y = StartY + (Row * (Size + Gap)) + HalfSize
            float yPos = startY + (spot.Y * (squareSize + gapY)) + (squareSize / 2);

            square.GlobalPosition = new Vector2(xPos, yPos);

            // --- TIMING ---
            // Fast paced! Warn for 1 beat, Active for 1 beat.
            square.WarningTime = _beatDuration * 1.0f;
            square.ActiveTime = _beatDuration * 1.0f;

            // Ensure they don't shoot bullets (reuse the SideSquare scene)
            square.ShootBullets = false;
        }
    }

    // Spawn a square that is adjacent to the screen edge
    private void SpawnSingleSquare_1(int slotID, float warn, float active, bool shoot)
    {
        var square = (StationaryHazard)SideSquareScene.Instantiate();
        AddChild(square);

        square.WarningTime = warn;
        square.ActiveTime = active;

        // Setup Shooting
        square.ShootBullets = shoot;
        square.BulletRef = BulletScene; // Pass the bullet scene!

        bool isLeft = slotID < 8;

        square.ShootDirection = isLeft ? Vector2.Right : Vector2.Left;

        int row = isLeft ? slotID : (slotID - 8);
        float yPos = (row * 135) + 67.5f;
        float xPos = isLeft ? 60 : 1920 - 60;

        square.GlobalPosition = new Vector2(xPos, yPos);
    }

    private async void SpawnRainSequence()
    {
        int drops = 4;
        for (int i = 0; i < drops; i++)
        {
            SpawnGravityRain();
            // Tiny delays between each drop
            await ToSignal(GetTree().CreateTimer(GD.RandRange(0.05, 0.1)), SceneTreeTimer.SignalName.Timeout);
            if (!IsInstanceValid(this)) return;
        }
    }

    private void SpawnGravityRain()
    {
        if (BulletScene == null) return;

        var bullet = (Bullet)BulletScene.Instantiate();
        AddChild(bullet);

        float randomX = GD.Randf() * 1920;
        // Start higher up so they accelerate before entering screen
        bullet.GlobalPosition = new Vector2(randomX, -100);

        bullet.Direction = Vector2.Down;
        bullet.Speed = 100; // Initial speed (slow)
        bullet.Gravity = 1200.0f; // High gravity for that heavy "drop" feel
    }

    private void ShakeCamera(float intensity)
    {
        float offsetX = (float)GD.RandRange(-intensity, intensity);
        float offsetY = (float)GD.RandRange(-intensity, intensity);

        MainCamera.Position = _screenCenter + new Vector2(offsetX, offsetY);
    }

    private void SpawnVerticalLasers()
    {
        if (VerticalLaserScene == null) return;

        int count = 4;
        float screenWidth = 1920.0f;
        float screenHeight = 1080.0f;

        // Divide screen into 4 columns
        float columnWidth = screenWidth / count; // 480px

        for (int i = 0; i < count; i++)
        {
            var laser = (VerticalLaser)VerticalLaserScene.Instantiate();
            AddChild(laser);

            // Calculate Center of each column
            // Col 0: 240, Col 1: 720, etc.
            float xPos = (i * columnWidth) + (columnWidth / 2);

            // Y Pos needs to be the BOTTOM of the screen (1080)
            // because the laser grows UP from (0,0)
            laser.GlobalPosition = new Vector2(xPos, screenHeight);

            // Timing
            laser.WarningTime = _beatDuration * 2.0f; // Warn for 2 beats
            laser.ActiveTime = _beatDuration * 4.0f;  // Stay for 4 beats
            laser.BeamWidth = 150.0f; // Make them thick!
        }

        // Optional: Also spawn the Pulse for chaos?
        // SpawnCirclePulse(); 
    }

    private void SpawnHorizontalLaser(bool fromRight)
    {
        var laser = (HorizontalLaser)HorizontalLaserScene.Instantiate();
        AddChild(laser);
        laser.FromRight = fromRight;

        // Random Y position
        float yPos = (float)GD.RandRange(100, 980);
        laser.GlobalPosition = new Vector2(fromRight ? 1920 : 0, yPos);

        laser.WarningTime = _beatDuration * 2.0f;
    }

    private void SpawnCornerCone(bool topRight)
    {
        Vector2 corner = topRight ? new Vector2(1920, 0) : new Vector2(0, 0);

        Vector2 screenCenter = new Vector2(960, 540);
        Vector2 dirToCenter = (screenCenter - corner).Normalized();
        float baseAngle = dirToCenter.Angle();

        float randomDriftDegrees = (float)GD.RandRange(-10.0, 10.0);
        float randomDriftRadians = Mathf.DegToRad(randomDriftDegrees);

        // Apply drift
        float finalCenterAngle = baseAngle + randomDriftRadians;

        // Debug Print: Check your Output tab to see these numbers changing!
        GD.Print($"Cone Drift: {randomDriftDegrees:F2} degrees");

        // 4. Configure Cone
        int laserCount = 4;
        float totalSpread = Mathf.DegToRad(60);

        float startAngle = finalCenterAngle - (totalSpread / 2);
        float angleStep = totalSpread / (laserCount - 1);

        for (int i = 0; i < laserCount; i++)
        {
            if (ThinLaserScene == null) return;

            var laser = (ThinLaser)ThinLaserScene.Instantiate();
            AddChild(laser);

            laser.GlobalPosition = corner;

            float currentAngle = startAngle + (angleStep * i);
            laser.Rotation = currentAngle;

            laser.WarningTime = _beatDuration * 2.0f;
            laser.ActiveTime = _beatDuration * 1.0f;
        }
    }

    private void SpawnAimedBullets()
    {
        Vector2[] corners = { new Vector2(0, 0), new Vector2(1920, 0) };

        foreach (var corner in corners)
        {
            if (_playerRef == null) return; // Safety check

            Vector2 target = _playerRef.GlobalPosition;
            Vector2 dirToPlayer = (target - corner).Normalized();
            float baseAngle = dirToPlayer.Angle();

            for (int i = -2; i <= 2; i++)
            {
                if (BulletScene == null) continue;

                var bullet = (Bullet)BulletScene.Instantiate();
                // Do NOT AddChild yet!

                bullet.GlobalPosition = corner;

                // Calculate Direction FIRST
                float angle = baseAngle + (Mathf.DegToRad(5) * i);
                bullet.Direction = Vector2.FromAngle(angle);
                bullet.Speed = 500.0f;

                // AddChild LAST
                AddChild(bullet);
            }
        }
    }

    private void SpawnSnake()
    {
        if (SnakeScene == null) return;

        var snake = (Snake)SnakeScene.Instantiate();

        // --- STEP 1: CALCULATE POSITION FIRST ---
        float randomX = (float)GD.RandRange(100, 1820);
        snake.GlobalPosition = new Vector2(randomX, 1100);

        // --- STEP 2: CONFIGURE VARIABLES ---
        snake.Speed = 250;
        snake.Frequency = 2;
        snake.Amplitude = 50;

        if (GD.Randf() > 0.5f) snake.Amplitude *= -1;

        AddChild(snake);
    }

    private void PulseVisualizer(float intensity = 1.0f)
    {
        if (VisualizerContainer == null) return;

        int index = 0;
        foreach (Control bar in VisualizerContainer.GetChildren())
        {
            // 1. Calculate a random height for this specific bar
            // "Noise" look: Some bars jump high, some stay low.
            float randomHeight = (float)GD.RandRange(40, 150) * intensity;

            // 2. Create a specific tween for this bar
            Tween t = CreateTween();

            // 3. JUMP UP (Fast expansion)
            // We animate 'custom_minimum_size:y' because HBoxContainer controls the normal 'size'.
            t.TweenProperty(bar, "custom_minimum_size:y", randomHeight, 0.05f)
                .SetTrans(Tween.TransitionType.Circ)
                .SetEase(Tween.EaseType.Out);

            // 4. SHRINK DOWN (Slow decay)
            // Go back to resting height (e.g., 20px)
            t.TweenProperty(bar, "custom_minimum_size:y", 20.0f, 0.3f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);

            index++;
        }
    }

    public override void _ExitTree()
    {
        if (Conductor.Instance != null)
        {
            Conductor.Instance.Stop();
            // Unsubscribe only from what we used
            Conductor.Instance.BeatHit -= OnBeatHit;
        }
    }
}