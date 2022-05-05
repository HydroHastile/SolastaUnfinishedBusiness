﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SolastaMulticlass.Models;
using static SolastaCommunityExpansion.Models.RespecContext;

namespace SolastaCommunityExpansion.Models
{
    internal static class LevelDownContext
    {
        public class FunctorLevelDown : Functor
        {
            public override IEnumerator Execute(
                FunctorParametersDescription functorParameters,
                FunctorExecutionContext context)
            {
                if (Global.IsMultiplayer)
                {
                    Gui.GuiService.ShowMessage(
                        MessageModal.Severity.Informative1,
                        "MainMenu/&ExportPdfTitle", "Message/&LevelDownMultiplayerAbortDescription",
                        "Message/&MessageOkTitle", string.Empty,
                        null, null);

                    yield break;
                }

                var state = -1;

                Gui.GuiService.ShowMessage(
                    MessageModal.Severity.Attention2,
                    "MainMenu/&ExportPdfTitle", "Message/&LevelDownConfirmationDescription",
                    "Message/&MessageYesTitle", "Message/&MessageNoTitle",
                    new MessageModal.MessageValidatedHandler(() => state = 1),
                    new MessageModal.MessageCancelledHandler(() => state = 0));

                while (state < 0)
                {
                    yield return null;
                }

                if (state > 0)
                {
                    if (functorParameters.RestingHero.ClassesHistory.Count > 1)
                    {
                        LevelDown(functorParameters.RestingHero);
                    }
                    else
                    {
                        yield return new FunctorRespec().Execute(functorParameters, context);
                    }
                }
            }
        }

        internal static void Load()
        {
            ServiceRepository.GetService<IFunctorService>().RegisterFunctor("LevelDown", new FunctorLevelDown());
        }

        internal static void ConfirmAndExecute(string filename)
        {
            var service = ServiceRepository.GetService<ICharacterPoolService>();

            service.LoadCharacter(filename, out var rulesetCharacterHero, out _);

            Gui.GuiService.ShowMessage(
                MessageModal.Severity.Attention2,
                "MainMenu/&ExportPdfTitle", "Message/&LevelDownConfirmationDescription",
                "Message/&MessageYesTitle", "Message/&MessageNoTitle",
                () => LevelDown(rulesetCharacterHero), null);
        }

        internal static void LevelDown(RulesetCharacterHero hero)
        {
            var indexLevel = hero.ClassesHistory.Count - 1;
            var characterClassDefinition = hero.ClassesHistory.Last();
            var classLevel = hero.ClassesAndLevels[characterClassDefinition];
            var classTag = AttributeDefinitions.GetClassTag(characterClassDefinition, classLevel);
            var subclassTag = string.Empty;

            hero.ClassesAndSubclasses.TryGetValue(characterClassDefinition, out var characterSubclassDefinition);

            if (characterSubclassDefinition != null)
            {
                subclassTag = AttributeDefinitions.GetSubclassTag(characterClassDefinition, classLevel, characterSubclassDefinition);
            }

            LevelUpContext.RegisterHero(hero);
            LevelUpContext.SetSelectedClass(hero, characterClassDefinition);
            LevelUpContext.SetSelectedSubclass(hero, characterSubclassDefinition);

            UnlearnSpells(hero, indexLevel);

            if (hero.ActiveFeatures.ContainsKey(subclassTag))
            {
                CustomFeaturesContext.RecursiveRemoveCustomFeatures(hero, hero.ActiveFeatures[subclassTag], subclassTag);

                hero.ActiveFeatures.Remove(subclassTag);
                hero.ClearFeatureModifiers(subclassTag);
            }

            if (hero.ActiveFeatures.ContainsKey(classTag))
            {
                CustomFeaturesContext.RecursiveRemoveCustomFeatures(hero, hero.ActiveFeatures[classTag], classTag);

                hero.ActiveFeatures.Remove(classTag);
                hero.ClearFeatureModifiers(classTag);
            }

            hero.RemoveClassLevel();
            hero.RefreshActiveFightingStyles();
            hero.RefreshActiveItemFeatures();
            hero.RefreshArmorClass();
            hero.RefreshAttackModes();
            hero.RefreshAttributeModifiersFromConditions();
            hero.RefreshAttributeModifiersFromFeats();
            hero.RefreshAttributes();
            hero.RefreshClimbRules();
            hero.RefreshConditionFlags();
            hero.RefreshEncumberance();
            hero.RefreshJumpRules();
            hero.RefreshMoveModes();
            hero.RefreshPersonalityFlags();
            hero.RefreshPowers();
            hero.RefreshProficiencies();
            hero.RefreshSpellRepertoires();
            hero.RefreshTags();
            hero.RefreshUsableDeviceFunctions();
            hero.ComputeHitPoints(true);

            LevelUpContext.UnregisterHero(hero);

            if (Gui.Game == null)
            {
                ServiceRepository.GetService<ICharacterPoolService>().SaveCharacter(hero, true);
            }
        }

        private static void UnlearnSpells(RulesetCharacterHero hero, int indexLevel)
        {
            var heroRepertoire = hero.SpellRepertoires.FirstOrDefault(x => LevelUpContext.IsRepertoireFromSelectedClassSubclass(hero, x));

            if (heroRepertoire == null)
            {
                return;
            }

            var cantripsToRemove = heroRepertoire.SpellCastingFeature.KnownCantrips[indexLevel] - heroRepertoire.SpellCastingFeature.KnownCantrips[indexLevel - 1];

            heroRepertoire.PreparedSpells.Clear();

            while (cantripsToRemove-- > 0)
            {
                heroRepertoire.KnownCantrips.RemoveAt(heroRepertoire.KnownCantrips.Count - 1);
            }

            switch (heroRepertoire.SpellCastingFeature.SpellKnowledge)
            {
                case RuleDefinitions.SpellKnowledge.Spellbook:
                    var scribbedSpellsToRemove = heroRepertoire.SpellCastingFeature.ScribedSpells[indexLevel];
                    var rulesetItemSpellbooks = new List<RulesetItemSpellbook>();

                    hero.CharacterInventory.BrowseAllCarriedItems(rulesetItemSpellbooks);

                    if (rulesetItemSpellbooks.Count > 0)
                    {
                        var rulesetItemSpellbook = rulesetItemSpellbooks[0];

                        while (scribbedSpellsToRemove-- > 0)
                        {
                            rulesetItemSpellbook.ScribedSpells.RemoveAt(rulesetItemSpellbook.ScribedSpells.Count - 1);
                        }
                    }

                    break;

                case RuleDefinitions.SpellKnowledge.Selection:
                    var spellsToRemove = heroRepertoire.SpellCastingFeature.KnownSpells[indexLevel] - heroRepertoire.SpellCastingFeature.KnownSpells[indexLevel - 1];

                    while (spellsToRemove-- > 0)
                    {
                        heroRepertoire.KnownSpells.RemoveAt(heroRepertoire.KnownSpells.Count - 1);
                    }

                    break;
            }
        }
    }
}
