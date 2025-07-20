using HarmonyLib;
using System.Collections.Generic;
using System;
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

[HarmonyPatch(typeof(Utility_HoldTracker))]
public static class Utility_HoldTracker_Notify_HoldTrackerItem_Patch
{
    static bool Prepare() => ExtendedLoadoutMod.Instance.useMultiLoadouts;

    [HarmonyPatch(nameof(Utility_HoldTracker.Notify_HoldTrackerItem))]
    [HarmonyPrefix]
    public static bool Notify_HoldTrackerItem(Pawn pawn, Thing item, int count)
    {
        if (LoadoutMulti_Manager.GetLoadout(pawn, false)!.defaultLoadout)
            return false;
        List<HoldRecord> holdRecordList = LoadoutManager.GetHoldRecords(pawn);
        if (holdRecordList == null)
        {
            holdRecordList = new List<HoldRecord>();
            LoadoutManager.AddHoldRecords(pawn, holdRecordList);
        }
            HoldRecord holdRecord1 = holdRecordList.FirstOrDefault<HoldRecord>((Predicate<HoldRecord>) (hr => hr.thingDef == item.def));
        if (holdRecord1 != null)
        {
        if (holdRecord1.pickedUp)
            holdRecord1.count = Utility_HoldTracker.GetMagazineAwareStackCount(pawn, holdRecord1.thingDef) + count;
        else
            holdRecord1.count += count;
        }
        else
        {
            HoldRecord holdRecord2 = new HoldRecord(item.def, count);
            holdRecordList.Add(holdRecord2);
        }
        return false;
    }
}