using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFECore
{
    [HarmonyPatch(typeof(HediffSet), nameof(HediffSet.BleedRateTotal), MethodType.Getter)]
    public static class Patch_BleedRateTotal
    {
        public static void Postfix(ref float __result, HediffSet __instance)
        {
            if (__result > 0 && __instance.pawn?.apparel?.WornApparel != null)
            {
                foreach (var apparel in __instance.pawn.apparel.WornApparel)
                {
                    var extension = apparel.def.GetModExtension<ApparelExtension>();
                    if (extension != null && extension.preventBleeding)
                    {
                        __result = 0;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SanguophageUtility), nameof(SanguophageUtility.DoBite), MethodType.Normal)]
        public static class Patch_DoBite
        {
            public static void Postfix(Pawn biter, Pawn victim)
            {
                var bloodLossDef = HediffDefOf.BloodLoss;
                var culpritHediff = victim.health.hediffSet.GetFirstHediffOfDef(bloodLossDef);
                if (culpritHediff?.Severity < bloodLossDef.lethalSeverity)
                    return;
                var thirstNeed = biter.needs?.TryGetNeed<Need_KillThirst>();
                if (thirstNeed != null)
                    thirstNeed.CurLevel = 1f;
            }
        }
    }
}