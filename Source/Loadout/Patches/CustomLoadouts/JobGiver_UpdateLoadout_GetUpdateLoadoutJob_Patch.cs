using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace CombatExtended.ExtendedLoadout;

[HarmonyPatch(typeof(JobGiver_UpdateLoadout), nameof(JobGiver_UpdateLoadout.GetUpdateLoadoutJob))]
public static class JobGiver_UpdateLoadout_GetUpdateLoadoutJob_Patch
{
    static bool Prepare() => ExtendedLoadoutMod.Instance.useMultiLoadouts;

    [HarmonyPrefix]
    public static bool RouteExcessInventoryDirectly(Pawn pawn, ref Job? __result)
    {
        if (pawn.Map == null || pawn.TryGetComp<CompInventory>()?.container == null)
        {
            return true;
        }

        if (pawn.equipment?.Primary is WeaponPlatform platform)
        {
            platform.TrySyncPlatformLoadout(pawn);
        }

        List<Thing> removed = new();

        // Preserve CE's priority: excess equipped weapons are handled first.
        if (pawn.GetExcessEquipment(out _)
            || !pawn.GetExcessThing(out Thing dropThing, out int dropCount)
            || dropThing == null
            || dropCount <= 0
            || !dropThing.def.EverStorable(true))
        {
            return true;
        }

        if (!StoreUtility.TryFindBestBetterStoreCellFor(
                dropThing,
                pawn,
                pawn.Map,
                StoragePriority.Unstored,
                pawn.Faction,
                out IntVec3 storeCell,
                true)
            || !pawn.CanReserve(storeCell, 1, -1))
        {
            // If there is no valid storage cell, retain CE's drop-on-ground fallback.
            return true;
        }

        Job job = JobMaker.MakeJob(
            ExtendedLoadout_JobDefOf.CE_Extended_UnloadLoadoutItem,
            dropThing,
            storeCell);
        job.count = Math.Min(dropCount, dropThing.stackCount);
        __result = job;
        return false;
    }
}
