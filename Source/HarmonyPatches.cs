using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Reflection;

namespace PreferPreferredApparelColor
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("llunak.PreferPreferredApparelColor");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
