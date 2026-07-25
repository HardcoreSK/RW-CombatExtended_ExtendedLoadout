using HarmonyLib;
using Verse;

namespace CombatExtended.ExtendedLoadout;

[HarmonyPatch(typeof(Utility_Loadouts))]
public static class Utility_Loadouts_GetLoadout_Patch
{
    static bool Prepare() => ExtendedLoadoutMod.Instance.useMultiLoadouts;

    [HarmonyPatch(nameof(Utility_Loadouts.GetLoadout))]
    [HarmonyPrefix]
    public static bool GetLoadout(Pawn pawn, ref Loadout __result)
    {
        __result = ResolveLoadout(pawn);
        return false;
    }

    internal static Loadout ResolveLoadout(Pawn pawn)
    {
        Loadout? loadout = LoadoutMulti_Manager.GetLoadout(pawn, false);
        if (loadout is not Loadout_Multi multiLoadout || multiLoadout.SlotCount > 0)
        {
            return loadout ?? LoadoutManager.DefaultLoadout;
        }

        // Combat Extended's GetLoadout contract is non-null. An empty multi-loadout
        // must behave like the default "Nothing" loadout, otherwise CE either
        // dereferences null or treats every carried item as excess.
        return LoadoutManager.DefaultLoadout;
    }
}

[HarmonyPatch(typeof(Utility_Loadouts))]
public static class Utility_Loadouts_GetLoadoutId_Patch
{
    static bool Prepare() => ExtendedLoadoutMod.Instance.useMultiLoadouts;

    [HarmonyPatch(nameof(Utility_Loadouts.GetLoadoutId))]
    [HarmonyPrefix]
    public static bool GetLoadoutId(Pawn pawn, ref int __result)
    {
        __result = Utility_Loadouts_GetLoadout_Patch.ResolveLoadout(pawn).UniqueID;
        return false;
    }
}
