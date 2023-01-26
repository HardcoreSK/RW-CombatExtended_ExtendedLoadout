﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace CombatExtended.ExtendedLoadout;

/// <summary>
/// Draw HP and Quality ranges
/// </summary>
[HarmonyPatch(typeof(Dialog_ManageLoadouts), nameof(Dialog_ManageLoadouts.DoWindowContents))]
[HotSwappable]
public class Dialog_ManageLoadouts_DoWindowContents_Patch
{
    static bool Prepare() => ExtendedLoadoutMod.Instance.useHpAndQualityInLoadouts;

    [HarmonyTranspiler]
    [UsedImplicitly]
    public static IEnumerable<CodeInstruction> DoWindowContents_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var drawSlotList = AccessTools.Method(typeof(Dialog_ManageLoadouts), "DrawSlotList");

        bool heightFixed = false;
        bool drawHpQualityInjected = false;
        foreach (var ci in instructions)
        {
            if (!heightFixed && ci.opcode == OpCodes.Ldc_R4 && (float)ci.operand == 48f)
            {
                // decrease slotListRect height
                ci.operand = 160f; // canvas3..ctor(0f, canvas2.yMax + 6f, (canvas.width - 6f) / 2f, canvas.height - 30f - canvas2.height - 48f - 30f);
                yield return ci;
                heightFixed = true;
            }
#pragma warning disable 252,253
            else if (heightFixed && !drawHpQualityInjected && ci.opcode == OpCodes.Call && ci.operand == drawSlotList)
#pragma warning restore 252,253
            {
                // draw after DrawSlotList(slotListRect);
                yield return ci;
                yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                yield return new CodeInstruction(OpCodes.Ldloc_S, 8); // local: bulkBarRect
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Dialog_ManageLoadouts_DoWindowContents_Patch), nameof(DrawHpQuality)));
                drawHpQualityInjected = true;
            }
            else
            {
                yield return ci;
            }
        }
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (!drawHpQualityInjected || !heightFixed)
        {
            Log.Error($"drawHpQualityInjected = {drawHpQualityInjected}; heightFixed = {heightFixed}");
        }
    }

    public static void DrawHpQuality(Dialog_ManageLoadouts dialog, Rect bulkBarRect)
    {
        Rect refillRect = new(bulkBarRect.xMin, bulkBarRect.yMax + 6f, bulkBarRect.width, 24f);
        Rect hpRect = new(refillRect.xMin, refillRect.yMax + 6f, refillRect.width, 24f);
        Rect qualityRect = new(hpRect.xMin, hpRect.yMax + 6f, hpRect.width, 24f);
        var loadoutExtended = dialog.CurrentLoadout.Extended();
        
        GUI.color = new Color(0.6f, 0.6f, 0.6f);
        loadoutExtended.RefillThreshold = Widgets.HorizontalSlider(refillRect, loadoutExtended.RefillThreshold, 0f, 1f, false, "CE_Extended.RefillThreshold".Translate(Mathf.RoundToInt(loadoutExtended.RefillThreshold * 100)), null, null, -1f);
        GUI.color = Color.white;
        
        Widgets.FloatRange(hpRect, 976833333, ref loadoutExtended.HpRange, 0f, 1f, "HitPoints", ToStringStyle.PercentZero);
        
        Widgets.QualityRange(qualityRect, 976833334, ref loadoutExtended.QualityRange);
    }
}