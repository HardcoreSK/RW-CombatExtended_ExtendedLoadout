using System.Collections.Generic;
using Verse;

namespace CombatExtended.ExtendedLoadout
{
    public class LoadoutCompatibilityDef : Def
    {
        public string? conditionType;
        public string? conditionPath;

        public bool noModSetttingConditions = false;

        public List<ThingDef>? thingDefs;
        public List<ThingCategoryDef>? thingCategories;

        public string? modExtensionType;

    }
}