using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Verse;
using Verse.AI;

namespace CombatExtended.ExtendedLoadout
{
    public static class CompatibilityUtility
    {

        private class CachedRule
        {
            public LoadoutCompatibilityDef Def = null!;
            public Type? ModExtensionType;
        }

        private static List<CachedRule>? exceptionDefs;

        public static void Resolve()
        {
            exceptionDefs = new List<CachedRule>();

            foreach (LoadoutCompatibilityDef def in DefDatabase<LoadoutCompatibilityDef>.AllDefsListForReading)
            {
                exceptionDefs.Add(new CachedRule
                {
                    Def = def,
                    ModExtensionType = def.modExtensionType.NullOrEmpty()
                        ? null
                        : GenTypes.GetTypeInAnyAssembly(def.modExtensionType),
                });
            }
        }

        public static bool ShouldIgnore(ThingDef thingDef)
        {
            //Log.Message($"Checking {thingDef.defName}");
            if (exceptionDefs == null)
            {
                Resolve();
            }

            List<CachedRule> rules = exceptionDefs!;

            foreach (CachedRule rule in rules)
            {
                if (!EvaluateCondition(rule.Def))
                    continue;

                if (Matches(rule, thingDef))
                {
                    //Log.Message($"Ignoring {thingDef.defName}");
                    return true;
                }
            }

            return false;
        }

        private static bool EvaluateCondition(LoadoutCompatibilityDef def)
        {
            if (def == null)
                return false;

            if (def.noModSetttingConditions)
                return true;

            if (def.conditionType == null || def.conditionPath == null)
                return false;

            return EvaluateBool(def.conditionType, def.conditionPath);
        }

        private static bool Matches(CachedRule rule, ThingDef thingDef)
        {
            LoadoutCompatibilityDef def = rule.Def;

            // Explicit ThingDefs
            if (def.thingDefs != null && def.thingDefs.Contains(thingDef))
                return true;

            // Categories
            if (def.thingCategories != null &&
                thingDef.thingCategories != null &&
                thingDef.thingCategories.Any(c => def.thingCategories.Contains(c)))
            {
                return true;
            }

            if (def.noModSetttingConditions)
                return false;

            // Cached mod extension type
            if (rule.ModExtensionType != null &&
                thingDef.modExtensions?.Any(ext => rule.ModExtensionType.IsInstanceOfType(ext)) == true)
            {
                return true;
            }

            if (rule.ModExtensionType == null  && !def.modExtensionType.NullOrEmpty())
            {
                Log.Warning($"[Extended Loadout] Could not resolve mod extension type '{def.modExtensionType}' and not noModSetttingConditions is not true");
            }

            return false;
        }

        private static bool EvaluateBool(string typeName, string memberPath)
        {
            if (typeName.NullOrEmpty() || memberPath.NullOrEmpty())
                return false;

            Type type = GenTypes.GetTypeInAnyAssembly(typeName);
            if (type == null)
                return false;

            object current = type;

            foreach (string memberName in memberPath.Split('.'))
            {
                if (current is Type currentType)
                {
                    // Static field
                    FieldInfo staticField = currentType.GetField(
                        memberName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (staticField != null)
                    {
                        current = staticField.GetValue(null);
                        continue;
                    }

                    // Static property
                    PropertyInfo staticProperty = currentType.GetProperty(
                        memberName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    if (staticProperty != null)
                    {
                        current = staticProperty.GetValue(null);
                        continue;
                    }

                    return false;
                }

                // Instance field
                FieldInfo instanceField = current.GetType().GetField(
                    memberName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (instanceField != null)
                {
                    current = instanceField.GetValue(current);
                    continue;
                }

                // Instance property
                PropertyInfo instanceProperty = current.GetType().GetProperty(
                    memberName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (instanceProperty != null)
                {
                    current = instanceProperty.GetValue(current);
                    continue;
                }

                return false;
            }

            return current is bool value && value;
        }

        // Copied from CE
        static public bool GetExcessThing_WithCompatibility(this Pawn pawn, out Thing dropThing, out int dropCount)
        {
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            Loadout loadout = pawn.GetLoadout();
            List<HoldRecord> records = LoadoutManager.GetHoldRecords(pawn);
            dropThing = null;
            dropCount = 0;

            if (inventory == null || inventory.container == null || loadout == null || loadout.defaultLoadout)
            {
                return false;
            }

            Dictionary<ThingDef, Integer> listing = pawn.GetStorageByThingDef();
            HashSet<ThingDef> inLoadout = new HashSet<ThingDef>();

            foreach (LoadoutSlot slot in loadout.GetSlotsFor(pawn))
            {
                if (slot.thingDef != null && listing.ContainsKey(slot.thingDef))
                {
                    listing[slot.thingDef].value -= slot.count;
                    if (listing[slot.thingDef].value <= 0)
                    {
                        listing.Remove(slot.thingDef);
                    }
                    else
                    {
                        inLoadout.Add(slot.thingDef);
                    }
                }
                if (slot.genericDef != null)
                {
                    List<ThingDef> killKeys = new List<ThingDef>();
                    int desiredCount = slot.count;
                    foreach (ThingDef def in listing.Keys.Where(td => slot.genericDef.lambda(td)))
                    {
                        listing[def].value -= desiredCount;
                        if (listing[def].value <= 0)
                        {
                            desiredCount = 0 - listing[def].value;
                            killKeys.Add(def);
                        }
                        else
                        {
                            inLoadout.Add(def);
                            break;
                        }
                    }
                    foreach (ThingDef def in killKeys)
                    {
                        listing.Remove(def);
                    }
                }
            }

            if (listing.Any())
            {
                if (records != null && !records.NullOrEmpty())
                {
                    foreach (ThingDef def in listing.Keys)
                    {
                        if (CompatibilityUtility.ShouldIgnore(def)) // HSK Our custom check for ignorables
                            continue;

                        HoldRecord rec = records.FirstOrDefault(r => r.thingDef == def);
                        if (rec == null)
                        {
                            if (loadout.dropUndefined || inLoadout.Contains(def))
                            {
                                dropThing = inventory.container.FirstOrDefault(t => t.def == def && !pawn.IsItemQuestLocked(t));
                                if (dropThing != null)
                                {
                                    dropCount = listing[def].value > dropThing.stackCount ? dropThing.stackCount : listing[def].value;
                                    return true;
                                }
                            }
                        }
                        else if (rec.count < listing[def].value)
                        {
                            dropThing = pawn.inventory.innerContainer.FirstOrDefault(t => t.def == def && !pawn.IsItemQuestLocked(t));
                            if (dropThing != null)
                            {
                                dropCount = listing[def].value - rec.count;
                                dropCount = dropCount > dropThing.stackCount ? dropThing.stackCount : dropCount;
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    IEnumerable<ThingDef> droppable;
                    if (loadout.dropUndefined)
                    {
                        droppable = listing.Keys;
                    }
                    else
                    {
                        droppable = inLoadout;
                    }
                    foreach (ThingDef def in droppable)
                    {
                        if (CompatibilityUtility.ShouldIgnore(def)) // HSK our custom chek
                            continue;

                        dropThing = inventory.container.FirstOrDefault(t => t.GetInnerIfMinified().def == def && !pawn.IsItemQuestLocked(t));
                        if (dropThing != null)
                        {
                            dropCount = listing[def].value > dropThing.stackCount ? dropThing.stackCount : listing[def].value;
                            return true;
                        }
                    }
                }
            } // else
            return false;
        }
    }
}