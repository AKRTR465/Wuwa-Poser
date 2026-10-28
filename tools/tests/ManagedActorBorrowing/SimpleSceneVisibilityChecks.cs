using System.Text.Json.Nodes;
using UnrealEngine;
using WwPoser.ColdLoad;

internal static class SimpleSceneVisibilityChecks
{
    public static int Run(string fixture, ACharacter pawn)
    {
        int checks = 0, instanceId = 1;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        void Throws(Action run, string message)
        {
            try { run(); } catch (InvalidOperationException) { checks++; return; }
            throw new Exception(message);
        }
        TsSimpleNpc Person(double x = 10, bool hidden = false)
        {
            var actor = new TsSimpleNpc { InstanceId = instanceId++, Location = new(pawn.Location.X + x, pawn.Location.Y, pawn.Location.Z), bHidden = hidden };
            SimpleNpcController.Instance.SimpleNpcElements.Add(actor); return actor;
        }
        string Begin(string name, string mode = "scene_characters\t100000\n")
        {
            var directory = Path.Combine(fixture, name); Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "actors.tsv"), "1\t10\t100\t150\t0\t0\tRole10Skin100\t0\n");
            File.WriteAllText(Path.Combine(directory, "scene_visibility.tsv"), mode);
            Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", directory);
            ManagedBridge.BindSession(); ManagedBridge.BeginTarget(); ManagedBridge.CheckLoaded(); ManagedBridge.ShowAtLocal(); return directory;
        }
        var person = Person(75000); var boundary = Person(100000); var outside = Person(100001); var preHidden = Person(hidden: true);
        Begin("scene-simple-legacy", "npc_monsters\t10000\n");
        Check(!person.bHidden && !boundary.bHidden && preHidden.bHidden, "legacy NPC-only preparations retain their original scope");
        ManagedBridge.Stop(); ManagedBridge.CheckReleased();
        Begin("scene-simple-200m-compatible", "scene_characters\t20000\n");
        Check(!person.bHidden && !boundary.bHidden, "older 200m preparations remain readable with their explicitly saved radius");
        ManagedBridge.Stop(); ManagedBridge.CheckReleased();

        // A second registry entry for one native actor must not acquire a second
        // visibility lease with an already-overridden hidden baseline.
        void RegisterCreature(AActor actor, int kind)
        {
            var index = CharacterModel._Instance.Handles.Count; var entity = new WorldEntity { Index = index, Id = (index << 16) | 1 };
            entity.Components.Add(typeof(CreatureDataComponent), new CreatureDataComponent { EntityType = kind });
            entity.Components.Add(typeof(CharacterActorComponent), new CharacterActorComponent { ActorInternal = actor, SkeletalMesh = new() });
            var handle = new EntityHandle { Entity = entity, Index = index, Id = entity.Id };
            CharacterModel._Instance.Handles.Add(handle); ObjectSystem._instance.Objects.Add(entity); ObjectSystem._instance.Versions.Add(1);
        }
        var duplicate = Person(); RegisterCreature(duplicate, 1);
        var distantNpc = new ACharacter { Location = new(pawn.Location.X + 85000, pawn.Location.Y, pawn.Location.Z), bHidden = false };
        var monsterBoundary = new ACharacter { Location = new(pawn.Location.X + 100000, pawn.Location.Y, pawn.Location.Z), bHidden = false };
        var monsterOutside = new ACharacter { Location = new(pawn.Location.X + 100001, pawn.Location.Y, pawn.Location.Z), bHidden = false };
        RegisterCreature(distantNpc, 1); RegisterCreature(monsterBoundary, 2); RegisterCreature(monsterOutside, 2);
        var protectedAlias = Person(); protectedAlias._NativePtr = pawn._NativePtr;
        var directory = Begin("scene-simple-1km");
        Check(person.bHidden && boundary.bHidden && !outside.bHidden, "independent scene people hide through 1km inclusive, but not beyond");
        Check(distantNpc.bHidden && monsterBoundary.bHidden && !monsterOutside.bHidden, "ordinary NPCs and monsters use the same 1km boundary");
        Check(duplicate.bHidden && !protectedAlias.bHidden, "duplicate actor routing stays hidden while performers remain protected");
        Check(preHidden.bHidden && !person.Tick && person.Interval == .5f, "scene people retain tick state and preexisting visibility");
        person.bHidden = false; ManagedBridge.MaintainStage(); Check(person.bHidden, "scene people are re-hidden after game-side visibility changes");
        person.Location.X = pawn.Location.X + 100001; ManagedBridge.MaintainStage(); Check(!person.bHidden, "leaving the 1km radius restores the captured visibility immediately");
        person.Location.X = pawn.Location.X + 75000; var arrival = Person(90000); Thread.Sleep(550); ManagedBridge.MaintainStage();
        Check(person.bHidden && arrival.bHidden, "returning and newly streamed scene people join during playback");
        var recycled = Person(); Thread.Sleep(550); ManagedBridge.MaintainStage();
        SimpleNpcController.Instance.SimpleNpcElements.Remove(recycled);
        var replacement = Person(); replacement._NativePtr = recycled._NativePtr; replacement.bHidden = false;
        // No new scan: a reused native address must not receive restoration for
        // the previous wrapper/instance when this scene stops.
        person.FailUnhide = 1;
        Throws(ManagedBridge.Stop, "failed scene-person restoration must remain pending");
        Check(!arrival.bHidden && !duplicate.bHidden && !distantNpc.bHidden && !monsterBoundary.bHidden && preHidden.bHidden && !replacement.bHidden, "restoration continues after one failure and preserves old hidden and reused actor states");
        Throws(ManagedBridge.CheckReleased, "release acknowledgement waits for scene-person restoration");
        ManagedBridge.Stop(); ManagedBridge.CheckReleased(); Check(!person.bHidden, "a later stop retries the failed scene-person restoration");
        var lifecycle = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "managed_actor_lifecycle.json")))!;
        var scene = lifecycle["scene_visibility"]!; var targets = scene["targets"]!.AsArray();
        Check(scene["radius_cm"]!.GetValue<double>() == 100000 && scene["include_simple_scene_people"]!.GetValue<bool>(), "lifecycle records the wider radius and independent scene people scope");
        Check(targets.Select(t => t!["actor"]!.GetValue<string>()).Distinct().Count() == targets.Count, "native actor addresses have exactly one visibility baseline across both registries");
        Check(targets.Any(t => t!["category"]?.GetValue<string>() == "simple_scene_person" && t["expired"]!.GetValue<bool>()), "recycled scene-person registration is recorded as expired");
        person.bHidden = false; ManagedBridge.MaintainStage(); Check(!person.bHidden, "stopped maintenance never re-hides scene people");
        var beforeFailure = Person(); var failDuringHide = Person(); failDuringHide.FailHide = 1;
        Throws(() => Begin("scene-simple-partial-start"), "partial scene-person hide failure is reported");
        Check(beforeFailure.bHidden, "partial startup can own earlier scene-person visibility");
        ManagedBridge.Stop(); ManagedBridge.CheckReleased();
        Check(!beforeFailure.bHidden && !failDuringHide.bHidden, "partial scene-person startup rolls back all acquired visibility");
        Check(person.Valid && boundary.Valid && arrival.Valid, "scene hiding and restoration never destroy people");
        try { Begin("scene-simple-invalid-range", "scene_characters\t100001\n"); throw new Exception("unsupported scene radius accepted"); }
        catch (InvalidDataException) { checks++; }
        Console.WriteLine($"PASS: {checks} independent scene-person visibility checks; 1km bounds, streaming, identity, deduplication and recovery; no game access.");
        return checks;
    }
}
