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
        // original => return Loadout or LoadoutManager.DefaultLoadout
        __result = LoadoutMulti_Manager.GetLoadout(pawn, true)!;
        return false;
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
        // original => return Loadout or LoadoutManager.DefaultLoadout
        __result = LoadoutMulti_Manager.GetLoadout(pawn, false)!.UniqueID;
        return false;
    }
}