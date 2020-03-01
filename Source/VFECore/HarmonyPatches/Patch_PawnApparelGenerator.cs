﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace VFECore
{

    public static class Patch_PawnApparelGenerator
    {

        public static class PossibleApparelSet
        {

            public static class manual_CoatButNoShirt
            {

                public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    #if DEBUG
                        Log.Message("PawnApparelGenerator.PossibleApparelSet.manual_CoatButNoShirt transpiler start (1 match todo)");
                    #endif

                    var instructionList = instructions.ToList();

                    var apparelLayerDefOfShellInfo = AccessTools.Field(typeof(RimWorld.ApparelLayerDefOf), nameof(RimWorld.ApparelLayerDefOf.Shell));
                    var apparelLayerDefOfOuterShellInfo = AccessTools.Field(typeof(ApparelLayerDefOf), nameof(ApparelLayerDefOf.VFEC_OuterShell));

                    for (int i = 0; i < instructionList.Count; i++)
                    {
                        var instruction = instructionList[i];

                        // Also have the generator consider OuterShell as an appropriate clothing layer
                        if (instruction.opcode == OpCodes.Beq_S)
                        {
                            var prevInstruction = instructionList[i - 1];
                            if (prevInstruction.opcode == OpCodes.Ldsfld && instruction.OperandIs(apparelLayerDefOfShellInfo))
                            {
                                #if DEBUG
                                    Log.Message("PawnApparelGenerator.PossibleApparelSet.manual_CoatButNoShirt match 1 of 1");
                                #endif
                                yield return instruction;
                                yield return instructionList[i - 2]; // apparelLayerDef
                                yield return new CodeInstruction(OpCodes.Ldsfld, apparelLayerDefOfOuterShellInfo); // ApparelLayerDefOf.OuterShell
                                instruction = instruction.Clone(); //  if (... || apparelLayerDef == ApparelLayerDefOf.OuterShell || ...)
                            }
                        }

                        yield return instruction;
                    }
                }

            }

            [HarmonyPatch(typeof(PawnApparelGenerator), nameof(PawnApparelGenerator.GenerateStartingApparelFor))]
            public static class GenerateStartingApparelFor
            {

                public static void Postfix(Pawn pawn)
                {
                    // Change the colour of appropriate apparel items to match the pawn's faction's colour
                    if (pawn.apparel != null && pawn.Faction != null && pawn.kindDef.apparelColor == Color.white)
                    {
                        var pawnKindDefExtension = PawnKindDefExtension.Get(pawn.kindDef);
                        foreach (var apparel in pawn.apparel.WornApparel)
                        {
                            // Check from ThingDefExtension
                            var thingDefExtension = ThingDefExtension.Get(apparel.def);
                            if (!thingDefExtension.useFactionColourForPawnKinds.NullOrEmpty() && thingDefExtension.useFactionColourForPawnKinds.Contains(pawn.kindDef))
                            {
                                apparel.SetColor(pawn.Faction.Color);
                                continue;
                            }

                            // Check from PawnKindDefExtension
                            var apparelProps = apparel.def.apparel;
                            foreach (var partGroupAndLayerPair in pawnKindDefExtension.FactionColourApparelWithPartAndLayersList)
                            {
                                if (apparelProps.bodyPartGroups.Contains(partGroupAndLayerPair.First) && apparelProps.layers.Contains(partGroupAndLayerPair.Second))
                                {
                                    apparel.SetColor(pawn.Faction.Color);
                                    break;
                                }
                            }

                        }
                    }
                }

            }

        }

    }

}
