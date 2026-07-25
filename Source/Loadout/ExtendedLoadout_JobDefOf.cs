using RimWorld;
using Verse;

namespace CombatExtended.ExtendedLoadout;

[DefOf]
public static class ExtendedLoadout_JobDefOf
{
    public static JobDef CE_Extended_UnloadLoadoutItem = null!;

    static ExtendedLoadout_JobDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ExtendedLoadout_JobDefOf));
    }
}
