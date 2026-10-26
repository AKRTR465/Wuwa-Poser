using UnrealEngine;
using WwPoser.ColdLoad;

internal static class PendingRemovalChecks
{
    public static int Run(string fixture, ACharacter pawn, EntityHandle handle)
    {
        var checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        void Throws(Action action, string message)
        {
            try { action(); } catch (InvalidOperationException) { checks++; return; }
            throw new Exception(message);
        }
        var mesh = pawn.Mesh;
        var entity = handle.Entity;
        var component = entity.GetComponent<CharacterActorComponent>();
        var initialLocation = pawn.Location;
        var initialHidden = pawn.bHidden; var initialVisible = mesh.bVisible; var initialMeshHidden = mesh.bHiddenInGame;
        void Begin(string name, bool borrowed)
        {
            handle.PendingRemoving = false;
            pawn.Location = initialLocation; pawn.bHidden = false; mesh.bVisible = true; mesh.bHiddenInGame = false;
            var directory = Path.Combine(fixture, "pending-removal-" + name); Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "actors.tsv"), borrowed
                ? "1\t10\t100\t150\t0\t0\tRole10Skin100\t0\n"
                : "1\t90\t900\t150\t0\t0\tRole90Skin900\t0\n");
            Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", directory);
            ManagedBridge.BindSession(); ManagedBridge.BeginTarget(); ManagedBridge.CheckLoaded(); ManagedBridge.ShowAtLocal();
            Check(handle.HoldEntityMap.Count == 1, "the original player has exactly one session hold: " + name);
            handle.PendingRemoving = true;
        }
        void CheckRestored(string name)
        {
            Check(pawn.Location.X == initialLocation.X && pawn.Location.Y == initialLocation.Y && pawn.Location.Z == initialLocation.Z &&
                !pawn.bHidden && mesh.bVisible && !mesh.bHiddenInGame, "the pending original player restores its saved state: " + name);
            Check(handle.PendingRemoving && handle.HoldEntityMap.Count == 0, "restoration returns the hold without clearing the game's removal request: " + name);
        }

        foreach (var borrowed in new[] { true, false })
        {
            var lease = borrowed ? "borrowed" : "nonperforming";
            Begin(lease, borrowed);
            Throws(ManagedBridge.MaintainStage, "pending removal remains ineligible for active maintenance: " + lease);
            ManagedBridge.Stop(); ManagedBridge.CheckReleased();
            CheckRestored(lease);
            // A repeated Stop must not replay old state after the lease was returned.
            pawn.Location = new(71, 72, 73); pawn.bHidden = true; mesh.bVisible = false;
            ManagedBridge.Stop(); ManagedBridge.CheckReleased();
            Check(pawn.Location.X == 71 && pawn.Location.Y == 72 && pawn.Location.Z == 73 && pawn.bHidden && !mesh.bVisible &&
                handle.HoldEntityMap.Count == 0, "repeated Stop does not mutate a released pending entity: " + lease);
        }

        // The PendingRemoving exception is only for the acquired identity. Even
        // reused wrappers/native addresses must not authorize a new generation.
        foreach (var borrowed in new[] { true, false })
        foreach (var change in new[] { "generation", "actor", "mesh", "component" })
        {
            var name = (borrowed ? "borrowed-" : "nonperforming-") + change;
            Begin(name, borrowed);
            var heldLocation = pawn.Location; var heldHidden = pawn.bHidden; var heldVisible = mesh.bVisible; var heldMeshHidden = mesh.bHiddenInGame;
            var id = handle.Id; var version = ObjectSystem._instance.Versions[handle.Index];
            var replacementActor = new ACharacter { Location = new(81, 82, 83), bHidden = true };
            var replacementMesh = new USkeletalMeshComponent { Owner = replacementActor, bVisible = false, bHiddenInGame = true };
            if (change == "generation")
            {
                handle.Id = entity.Id = (handle.Index << 16) | ((version + 1) & 65535);
                ObjectSystem._instance.Versions[handle.Index] = handle.Id & 65535;
            }
            else if (change == "actor") component.ActorInternal = replacementActor;
            else if (change == "mesh") component.SkeletalMesh = replacementMesh;
            else entity.Components[typeof(CharacterActorComponent)] = new CharacterActorComponent { ActorInternal = pawn, SkeletalMesh = mesh };

            Throws(ManagedBridge.Stop, "pending removal cannot bypass identity rejection: " + name);
            Throws(ManagedBridge.CheckReleased, "rejected identity retains recovery ownership: " + name);
            Check(handle.HoldEntityMap.Count == 1 && pawn.Location.X == heldLocation.X && pawn.Location.Y == heldLocation.Y &&
                pawn.Location.Z == heldLocation.Z && pawn.bHidden == heldHidden && mesh.bVisible == heldVisible && mesh.bHiddenInGame == heldMeshHidden,
                "identity rejection happens before restoring or releasing the original object: " + name);
            Check(replacementActor.Location.X == 81 && replacementActor.bHidden && !replacementMesh.bVisible && replacementMesh.bHiddenInGame,
                "replacement actor and mesh are never modified: " + name);

            // Repair only the synthetic identity fault; leave PendingRemoving
            // true so the same restoration retry must now complete successfully.
            handle.Id = entity.Id = id; ObjectSystem._instance.Versions[handle.Index] = version;
            component.ActorInternal = pawn; component.SkeletalMesh = mesh; entity.Components[typeof(CharacterActorComponent)] = component;
            ManagedBridge.Stop(); ManagedBridge.CheckReleased(); CheckRestored(name);
        }
        handle.PendingRemoving = false;
        pawn.Location = initialLocation; pawn.bHidden = initialHidden; mesh.bVisible = initialVisible; mesh.bHiddenInGame = initialMeshHidden;
        Console.WriteLine($"PASS: {checks} pending-removal restoration checks; no game access.");
        return checks;
    }
}
