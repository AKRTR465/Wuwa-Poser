namespace WWPoser.Services;

public sealed record SquadPlacement(double OffsetX = 0, double OffsetY = 0, double Height = 0, double Yaw = 0, bool InPlace = true);
// Stage coordinates are relative to the controlled character when playback starts:
// X to its right, Y ahead, Z above its floor (cm), Yaw relative to its facing.
public sealed record StagePosition(double X, double Y, double Z = 0, double Yaw = 0);
public sealed record PlaybackActorRequest(string BodySource, string? FaceSource = null, double? Scale = null, SquadPlacement? Placement = null, long RoleId = 0, bool Enabled = true, string? CharacterId = null, uint Slot = 0, StagePosition? Stage = null, string? GazeSource = null, double GazeStrength = 1);
public sealed record PlaybackSessionRequest(IReadOnlyList<PlaybackActorRequest> Actors, string? CameraSource = null, int CameraMode = 0, double Speed = 1, bool Loop = false, double DurationSeconds = 0, bool NativePhysics = true, bool Physics = false, double SpacingCm = 180, bool FormationNative = false, bool HideSceneCharacters = false, double GroundHeightCm = -2, double CameraTrackHeightCm = 0)
{
    // Authoring validation also accepts incomplete and empty preset drafts.
    internal void Validate()
    {
        static void Range(double value, double min, double max)
        {
            if (!double.IsFinite(value) || value < min || value > max) throw new InvalidDataException("表演包含超出范围的数值。");
        }
        if (Actors is null) throw new InvalidDataException("表演演员配置无效。");
        Range(Speed, .1, 4); Range(DurationSeconds, 0, 2147483); Range(SpacingCm, 0, 1000);
        Range(GroundHeightCm, -1000, 1000); Range(CameraTrackHeightCm, -10000, 10000);
        if (CameraMode is < 0 or > 2) throw new InvalidDataException("镜头模式无效。");
        PlaybackAssets.PhysicsMode(NativePhysics, Physics);
        foreach (var actor in Actors)
        {
            if (actor is null) throw new InvalidDataException("表演演员为空。");
            if (actor.Scale is { } scale) Range(scale, .000001, 100);
            Range(actor.GazeStrength, 0, 3);
            if (actor.Placement is { } p) { Range(p.OffsetX, -1000, 1000); Range(p.OffsetY, -1000, 1000); Range(p.Height, -1000, 1000); Range(p.Yaw, -180, 180); }
            if (actor.Stage is { } s) { Range(s.X, -5000, 5000); Range(s.Y, -5000, 5000); Range(s.Z, -1000, 1000); Range(s.Yaw, -180, 180); }
        }
    }
}
