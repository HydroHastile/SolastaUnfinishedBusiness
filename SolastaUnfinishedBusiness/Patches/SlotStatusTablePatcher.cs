﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.Extensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Models;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterClassDefinitions;

namespace SolastaUnfinishedBusiness.Patches;

public static class SlotStatusTablePatcher
{
    [HarmonyPatch(typeof(SlotStatusTable), "Bind")]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    public static class Bind_Patch
    {
        private static bool UniqueLevelSlots(
            FeatureDefinitionCastSpell featureDefinitionCastSpell,
            RulesetSpellRepertoire rulesetSpellRepertoire)
        {
            var hero = rulesetSpellRepertoire.GetCasterHero();

            //PATCH: displays slots on any multicaster hero so Warlocks can see their spell slots
            return featureDefinitionCastSpell.UniqueLevelSlots && !SharedSpellsContext.IsMulticaster(hero);
        }

        public static IEnumerable<CodeInstruction> Transpiler([NotNull] IEnumerable<CodeInstruction> instructions)
        {
            var uniqueLevelSlotsMethod = typeof(FeatureDefinitionCastSpell).GetMethod("get_UniqueLevelSlots");
            var myUniqueLevelSlotsMethod =
                new Func<FeatureDefinitionCastSpell, RulesetSpellRepertoire, bool>(UniqueLevelSlots).Method;

            return instructions.ReplaceCalls(uniqueLevelSlotsMethod, "SlotStatusTable.Bind",
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, myUniqueLevelSlotsMethod));
        }

        //PATCH: creates different slots colors and pop up messages depending on slot types
        public static void Postfix(
            SlotStatusTable __instance,
            RulesetSpellRepertoire spellRepertoire,
            int spellLevel)
        {
            // spellRepertoire is null during level up...
            if (spellRepertoire == null || spellLevel == 0)
            {
                return;
            }

            var heroWithSpellRepertoire = spellRepertoire.GetCasterHero();

            if (heroWithSpellRepertoire is null)
            {
                return;
            }

            spellRepertoire.GetSlotsNumber(spellLevel, out var totalSlotsRemainingCount, out var totalSlotsCount);

            MulticlassGameUiContext.PaintPactSlots(
                heroWithSpellRepertoire,
                totalSlotsCount,
                totalSlotsRemainingCount,
                spellLevel,
                __instance.table,
                (Global.InspectedHero != null && spellRepertoire.spellCastingClass == Warlock)
                || (Global.InspectedHero == null && !Main.Settings.DisplayPactSlotsOnSpellSelectionPanel));
        }
    }

    //PATCH: ensures slot colors are white before getting back to pool
    [HarmonyPatch(typeof(SlotStatusTable), "Unbind")]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    public static class Unbind_Patch
    {
        public static void Prefix(SlotStatusTable __instance)
        {
            MulticlassGameUiContext.PaintSlotsWhite(__instance.table);
        }
    }
}
