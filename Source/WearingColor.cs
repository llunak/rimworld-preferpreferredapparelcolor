using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;

// Fix at least 60% of preferred color calculations.
namespace PreferPreferredApparelColor
{
    [DefOf]
    public static class ApparelLayerDefOf
    {
            [MayRequire("PeteTimesSix.ResearchReinvented")]
            public static ApparelLayerDef Satchel;

            static ApparelLayerDefOf()
            {
                DefOfHelper.EnsureInitializedInCtor(typeof(ApparelLayerDefOf));
            }
    }

    [HarmonyPatch(typeof(ThoughtWorker_WearingColor))]
    public static class ThoughtWorker_WearingColor_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(CurrentStateInternal))]
        public static IEnumerable<CodeInstruction> CurrentStateInternal(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            for( int i = 0; i < codes.Count; ++i )
            {
                // Log.Message("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // return (float)num / (float)p.apparel.WornApparelCount >= 0.6f;
                // Change it to:
                // return (float)num / CurrentStateInternal_Hook(p.apparel) >= 0.599f;
                if( codes[ i ].opcode == OpCodes.Callvirt && codes[ i ].operand.ToString() == "Int32 get_WornApparelCount()"
                    && i + 3 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Conv_R4
                    && codes[ i + 2 ].opcode == OpCodes.Div
                    && codes[ i + 3 ].opcode == OpCodes.Ldc_R4 && codes[ i + 3 ].operand.ToString() == "0.6" )
                {
                    if( ApparelLayerDefOf.Satchel != null ) // Is Research Reinvented used?
                        codes[ i ] = new CodeInstruction( OpCodes.Call, typeof( ThoughtWorker_WearingColor_Patch )
                            .GetMethod(nameof( CurrentStateInternal_Hook )));
                    codes[ i + 3 ].operand = 0.599f; // 3f/5f >= 0.6f is actually false, so make it pass.
                    found = true;
                    break;
                }
            }
            if( !found )
                Log.Error( "PreferPreferredApparelColor: Failed to patch ThoughtWorker_WearingColor.CurrentStateInternal()" );
            return codes;
        }

        public static int CurrentStateInternal_Hook( Pawn_ApparelTracker apparel )
        {
            int num = 0;
            foreach( Apparel item in apparel.WornApparel )
            {
                // Do not count Research Reinvented items in its new apparel layer,
                // the extra layer makes some setups miss the 60% requirement that would be met in vanilla.
                if( item.def.apparel.layers.Contains( ApparelLayerDefOf.Satchel ))
                    continue;
                ++num;
            }
            return num;
        }
    }
}
