using HarmonyLib;
using System.Linq;
using Verse;

namespace CombatExtended.ExtendedLoadout
{
    [HarmonyPatch(typeof(Utility_HoldTracker), nameof(Utility_HoldTracker.GetExcessThing))]
    public static class GetExcessThing_Patch
    {
        static bool Prefix(
            Pawn pawn,
            ref bool __result,
            ref Thing dropThing,
            ref int dropCount)
        {
            // Fast path - no compatibility rules apply.
            if (!pawn.inventory.innerContainer.Any(t => CompatibilityUtility.ShouldIgnore(t.def)))
                return true; // Run CE's original method.

            // Slow path - run our copy of GetExcessThing().
            __result = CompatibilityUtility.GetExcessThing_WithCompatibility(pawn, out dropThing, out dropCount);
            return false;
        }
    }
}