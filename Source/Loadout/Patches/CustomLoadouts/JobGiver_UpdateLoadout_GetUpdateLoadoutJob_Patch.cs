using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.ExtendedLoadout;

[HarmonyPatch(typeof(JobGiver_UpdateLoadout), nameof(JobGiver_UpdateLoadout.GetUpdateLoadoutJob))]
public static class JobGiver_UpdateLoadout_GetUpdateLoadoutJob_Patch
{
    static bool Prepare() => ExtendedLoadoutMod.Instance.useMultiLoadouts;

    [HarmonyPostfix]
    public static void RejectUnreservableExcessHaul(Pawn pawn, ref Job? __result)
    {
        if (__result?.def != JobDefOf.HaulToCell || __result.targetA.Thing == null)
        {
            return;
        }

        // The CE excess-item path drops the item first, then immediately creates
        // a haul job. The dropped item can merge into an already-reserved stack.
        // Reject that job before StartJob so RimWorld does not emit reservation
        // errors; normal hauling can pick the item up after the reservation clears.
        if (!pawn.CanReserve(__result.targetA, 1, -1))
        {
            __result = null;
        }
    }
}
