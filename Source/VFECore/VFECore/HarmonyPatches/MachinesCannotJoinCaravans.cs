﻿using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using VFE.Mechanoids.Needs;
using VFEMech;

namespace VFE.Mechanoids
{
    public class MechanoidExtension : DefModExtension
    {
        public bool isCaravanRiddable;
    }

	[HarmonyPatch]
	public static class CaravanUIUtility_AddPawnsSections_Patch
	{
		[HarmonyTargetMethod]
		public static MethodBase TargetMethod()
		{
			return typeof(CaravanUIUtility).GetNestedTypes(AccessTools.all).First(x => x.Name.Contains("<>c")).GetMethods(AccessTools.all).Where(x => x.Name.Contains("AddPawnsSections")).Last();
		}
		public static void Postfix(TransferableOneWay x, ref bool __result)
		{
			if (!__result && x.AnyThing is Pawn pawn)
			{
				__result = pawn.def.GetModExtension<MechanoidExtension>()?.isCaravanRiddable ?? false;
				if (pawn.guest is null)
				{
					pawn.guest = new Pawn_GuestTracker(pawn);
				}
			}
		}
	}

	[HarmonyPatch(typeof(CaravanFormingUtility), "AllSendablePawns")]
    public static class MachinesCannotJoinCaravans
    {
        public static void Postfix(ref List<Pawn> __result)
        {
            __result = __result.FindAll(pawn => pawn.def.thingClass != typeof(Machine) || (pawn.def.GetModExtension<MechanoidExtension>()?.isCaravanRiddable ?? false));
        }
    }

	[HarmonyPatch(typeof(Caravan_NeedsTracker), "TrySatisfyPawnNeeds")]
	public static class TrySatisfyPawnNeeds_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static bool Prefix(Caravan_NeedsTracker __instance, Pawn pawn)
		{
			var extension = pawn.def.GetModExtension<MechanoidExtension>();
			if (extension != null && pawn.def.GetModExtension<MechanoidExtension>().isCaravanRiddable)
			{
				TrySatisfyPawnNeeds(__instance, pawn);
				return false;
			}
			return true;
		}

		private static void TrySatisfyPawnNeeds(Caravan_NeedsTracker __instance, Pawn pawn)
		{
			if (pawn.Dead)
			{
				return;
			}
			List<Need> allNeeds = pawn.needs.AllNeeds;
			for (int i = 0; i < allNeeds.Count; i++)
			{
				Need need = allNeeds[i];
				Need_Power need_Power = need as Need_Power;
				if (need_Power != null)
				{
					TrySatisfyPowerNeed(__instance.caravan, pawn, need_Power);
				}
			}
		}

		private static void TrySatisfyPowerNeed(Caravan caravan, Pawn pawn, Need_Power powerNeed)
		{
			if ((int)powerNeed.CurCategory < 1)
			{
				return;
			}
			var food = CaravanInventoryUtility.AllInventoryItems(caravan).FirstOrDefault(x => x.def == ThingDefOf.Uranium);
			if (food != null)
			{
				var owner = CaravanInventoryUtility.GetOwnerOf(caravan, food);
				powerNeed.CurLevel += IngestedUranium(food, pawn, powerNeed.MaxLevel - powerNeed.CurLevel);
				if (food.Destroyed)
				{
					if (owner != null)
					{
						owner.inventory.innerContainer.Remove(food);
						caravan.RecacheImmobilizedNow();
						caravan.RecacheDaysWorthOfFood();
					}
				}
			}
		}

		public const float NutritionPerCount = 5f;
		private static float IngestedUranium(Thing thing, Pawn pawn, float nutritionWanted)
		{
			int stackConsumed = (int)Mathf.Min(nutritionWanted * NutritionPerCount, thing.stackCount / NutritionPerCount);
			if (thing.stackCount > stackConsumed)
			{
				thing.SplitOff(stackConsumed);
			}
			else
			{
				thing.Destroy();
			}
			return stackConsumed / NutritionPerCount;

		}
	}
}
