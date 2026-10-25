using System.Text.Json.Nodes;
using UnrealEngine;
using WwPoser.ColdLoad;

internal static class SceneVisibilityChecks
{
    public static int Run(string fixture, ACharacter pawn, EntityHandle teamMember)
    {
        var checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        void Throws(Action action, string message)
        {
            try { action(); } catch (InvalidOperationException) { checks++; return; }
            throw new Exception(message);
        }
        (ACharacter Actor, EntityHandle Handle) SceneActor(int kind, double x = 10, bool hidden = false)
        {
            var actor = new ACharacter { Location = new(pawn.Location.X + x, pawn.Location.Y, pawn.Location.Z), bHidden = hidden };
            actor.Mesh = new() { SkeletalMesh = new() { Name = "SceneActor" } };
            var index = CharacterModel._Instance.Handles.Count;
            var entity = new WorldEntity { Index = index, Id = (index << 16) | 1 };
            entity.Components.Add(typeof(CreatureDataComponent), new CreatureDataComponent { EntityType = kind });
            entity.Components.Add(typeof(CharacterActorComponent), new CharacterActorComponent { ActorInternal = actor, SkeletalMesh = actor.Mesh });
            var handle = new EntityHandle { Id = entity.Id, Index = index, Entity = entity };
            CharacterModel._Instance.Handles.Add(handle); ObjectSystem._instance.Objects.Add(entity); ObjectSystem._instance.Versions.Add(1);
            return (actor, handle);
        }
        string Begin(string name, bool enabled)
        {
            var directory = Path.Combine(fixture, name); Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "actors.tsv"), "1\t10\t100\t150\t0\t0\tRole10Skin100\t0\n");
            if (enabled) File.WriteAllText(Path.Combine(directory, "scene_visibility.tsv"), "npc_monsters\t10000\n");
            Environment.SetEnvironmentVariable("WWPOSER_OWNED_SESSION_DIR", directory);
            ManagedBridge.BindSession(); ManagedBridge.BeginTarget(); ManagedBridge.CheckLoaded(); ManagedBridge.ShowAtLocal();
            return directory;
        }
        var npc = SceneActor(1); var monster = SceneActor(2); var animal = SceneActor(8); var remotePlayer = SceneActor(0);
        var farMonster = SceneActor(2, 20000); var alreadyHidden = SceneActor(1, hidden: true);
        Begin("scene-hide-disabled", false);
        Check(!npc.Actor.bHidden && !monster.Actor.bHidden && alreadyHidden.Actor.bHidden, "legacy preparations do not alter scene actor visibility");
        ManagedBridge.Stop(); ManagedBridge.CheckReleased();

        var teammateActor = teamMember.Entity.GetComponent<CharacterActorComponent>().ActorInternal;
        var teammateData = teamMember.Entity.GetComponent<CreatureDataComponent>();
        var previousType = teammateData.EntityType; var previousHidden = teammateActor.bHidden;
        teammateData.EntityType = 1; teammateActor.bHidden = false;
        var directory = Begin("scene-hide-enabled", true);
        Check(npc.Actor.bHidden && monster.Actor.bHidden, "nearby NPC and monster models hide at playback startup");
        Check(!animal.Actor.bHidden && !remotePlayer.Actor.bHidden && !farMonster.Actor.bHidden, "animals, other players and distant monsters stay unchanged");
        Check(!pawn.bHidden && !teammateActor.bHidden, "performers and verified teammates remain protected even with an NPC category");
        Check(alreadyHidden.Actor.bHidden && !npc.Actor.Tick && npc.Actor.Interval == .5f, "hiding preserves prior hidden state and tick behavior");
        Check(npc.Handle.HoldEntityMap.Count == 0 && monster.Handle.HoldEntityMap.Count == 0, "scene visibility never retains or destroys world entities");
        npc.Actor.bHidden = false; ManagedBridge.MaintainStage();
        Check(npc.Actor.bHidden, "maintenance repairs game-side visibility overwrites");
        var arrival = SceneActor(1); Thread.Sleep(550); ManagedBridge.MaintainStage();
        Check(arrival.Actor.bHidden, "NPCs streamed into range during playback are discovered");
        // Reuse every managed wrapper and native Actor address, but advance the
        // entity generation: cached references alone must not permit restoration.
        var oldId = monster.Handle.Id;
        monster.Handle.Id = monster.Handle.Entity.Id = (monster.Handle.Index << 16) | 2;
        ObjectSystem._instance.Versions[monster.Handle.Index] = 2;
        npc.Handle.PendingRemoving = true; npc.Actor.FailUnhide = 1;
        Throws(ManagedBridge.Stop, "failed NPC restore must remain retryable");
        Check(!arrival.Actor.bHidden && alreadyHidden.Actor.bHidden, "one restore failure does not skip other NPCs or unhide preexisting hidden actors");
        Check(monster.Actor.bHidden, "a recycled entity generation is never unhidden using an old lease");
        Throws(ManagedBridge.CheckReleased, "scene visibility restoration participates in release acknowledgement");
        ManagedBridge.Stop(); ManagedBridge.CheckReleased();
        Check(!npc.Actor.bHidden, "pending-removal original entities restore while their identity is still registered");
        Check(npc.Actor.Valid && arrival.Actor.Valid && monster.Actor.Valid, "restoration never destroys a scene actor");
        var status = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "managed_actor_lifecycle.json")))!;
        Check(status["all_released"]!.GetValue<bool>(), "final lifecycle requires all NPC visibility leases to retire");
        var targets = status["scene_visibility"]!["targets"]!.AsArray();
        Check(targets.Any(row => row!["entity_id"]!.GetValue<int>() == oldId && row["expired"]!.GetValue<bool>()), "lifecycle preserves the captured identity and expired-generation evidence");
        npc.Actor.bHidden = false; ManagedBridge.MaintainStage();
        Check(!npc.Actor.bHidden, "maintenance never re-hides NPCs after stopping");
        teammateData.EntityType = previousType; teammateActor.bHidden = previousHidden;
        var beforeFailure = SceneActor(1); var failDuringHide = SceneActor(2); failDuringHide.Actor.FailHide = 1;
        Throws(() => Begin("scene-hide-partial-start", true), "a partially hidden scene reports a startup failure");
        Check(beforeFailure.Actor.bHidden, "a startup failure can occur after earlier NPCs were hidden");
        ManagedBridge.Stop(); ManagedBridge.CheckReleased();
        Check(!beforeFailure.Actor.bHidden && !failDuringHide.Actor.bHidden, "startup rollback restores both successful and failed hide acquisitions");
        Console.WriteLine($"PASS: {checks} NPC/monster visibility checks; no game access.");
        return checks;
    }
}
