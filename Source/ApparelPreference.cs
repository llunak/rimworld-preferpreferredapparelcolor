using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;

// Make the automatic apparel selection algorithm score apparel matching a preferred color a bit higher.
namespace PreferPreferredApparelColor
{
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel))]
    public static class JobGiver_OptimizeApparel_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(ApparelScoreRaw))]
        public static IEnumerable<CodeInstruction> ApparelScoreRaw(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            for( int i = 0; i < codes.Count; ++i )
            {
                // Log.Message("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // Function has code:
                // if (ap.WornByCorpse
                // Prepend:
                // num += ApparelScoreRaw_Hook( ap, pawn );
                if( codes[ i ].IsLdarg()
                    && i + 1 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Callvirt && codes[ i + 1 ].operand.ToString() == "Boolean get_WornByCorpse()" )
                {
                    codes.Insert( i + 1, new CodeInstruction( OpCodes.Ldarg_0 )); // load 'pawn'
                    codes.Insert( i + 2, new CodeInstruction( OpCodes.Ldarg_1 )); // load 'ap'
                    codes.Insert( i + 3, new CodeInstruction( OpCodes.Ldloca, 0 )); // load 'num' address
                    codes.Insert( i + 4, new CodeInstruction( OpCodes.Call, typeof( JobGiver_OptimizeApparel_Patch )
                        .GetMethod(nameof( ApparelScoreRaw_Hook ))));
                    found = true;
                    break;
                }
            }
            if( !found )
                Log.Error( "PreferPreferredApparelColor: Failed to patch JobGiver_OptimizeApparel_Patch.ApparelScoreRaw()" );
            return codes;
        }

        public static void ApparelScoreRaw_Hook( Pawn p, Apparel ap, ref float num )
        {
            if( p.DevelopmentalStage.Baby())
                return;
            Color? favColor = p.story?.favoriteColor?.color;
            Color? ideoColor = p.Ideo?.ApparelColor;
            if( !favColor.HasValue && !ideoColor.HasValue )
                return;
            CompColorable compColorable = ap.TryGetComp<CompColorable>();
            if (compColorable != null && compColorable.Active )
            {
                if( favColor.HasValue && compColorable.Color.IndistinguishableFrom( favColor.Value ))
                    num += 0.12f; // Same as human leather bonus.
                else if( ideoColor.HasValue && compColorable.Color.IndistinguishableFrom( ideoColor.Value ))
                    num += 0.12f;
            }
        }
    }
}
