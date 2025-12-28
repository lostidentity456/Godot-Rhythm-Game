using Godot;

public partial class Conductor : AudioStreamPlayer
{
    // Singleton Instance
    public static Conductor Instance { get; private set; }

    // Signals
    [Signal] public delegate void BeatHitEventHandler(int beatNumber);
    [Signal] public delegate void MeasureHitEventHandler(int measureNumber);

    [Export] public float Bpm = 120.0f;
    [Export] public int Measures = 4;

    // Tracking
    public float SongPosition = 0.0f;
    public float SongPositionInBeats = 0.0f;
    private float _secPerBeat = 60.0f;
    private int _lastReportedBeat = 0;
    private int _measure = 1;

    public override void _Ready()
    {
        Instance = this;
        _secPerBeat = 60.0f / Bpm;
    }

    public override void _Process(double delta)
    {
        if (Playing)
        {
            // Calculate precise song position
            SongPosition = GetPlaybackPosition()
                + (float)AudioServer.GetTimeSinceLastMix()
                - (float)AudioServer.GetOutputLatency();
            
            SongPositionInBeats = (int)(SongPosition / _secPerBeat);
            
            ReportBeat();
        }
    }

    private void ReportBeat()
    {
        if (_lastReportedBeat < SongPositionInBeats)
        {
            if (_measure > Measures)
            {
                _measure = 1;
            }

            // Emit Signal
            EmitSignal(SignalName.BeatHit, (int)SongPositionInBeats);
            EmitSignal(SignalName.MeasureHit, _measure);

            _lastReportedBeat = (int)SongPositionInBeats;
            _measure += 1;
        }
    }

    public void PlayWithBeatOffset(float offset = 0.0f)
    {
        Play(offset);
    }
}