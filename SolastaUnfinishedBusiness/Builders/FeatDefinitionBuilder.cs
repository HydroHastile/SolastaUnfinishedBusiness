﻿using System;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.Infrastructure;

namespace SolastaUnfinishedBusiness.Builders;

internal abstract class FeatDefinitionBuilder<TDefinition, TBuilder> : DefinitionBuilder<TDefinition, TBuilder>
    where TDefinition : FeatDefinition
    where TBuilder : FeatDefinitionBuilder<TDefinition, TBuilder>
{
    internal TBuilder SetFeatures(params FeatureDefinition[] features)
    {
        Definition.Features.SetRange(features);
        Definition.Features.Sort(Sorting.Compare);
        return This();
    }

    internal TBuilder AddFeatures(params FeatureDefinition[] features)
    {
        Definition.Features.AddRange(features);
        Definition.Features.Sort(Sorting.Compare);
        return This();
    }

    internal TBuilder SetAbilityScorePrerequisite(string abilityScore, int value)
    {
        Definition.minimalAbilityScorePrerequisite = true;
        Definition.minimalAbilityScoreName = abilityScore;
        Definition.minimalAbilityScoreValue = value;
        return This();
    }

    internal TBuilder SetMustCastSpellsPrerequisite()
    {
        Definition.mustCastSpellsPrerequisite = true;
        return This();
    }

    internal TBuilder SetFeatFamily(string family)
    {
        if (string.IsNullOrEmpty(family))
        {
            Definition.hasFamilyTag = false;
            Definition.familyTag = string.Empty;
        }
        else
        {
            Definition.hasFamilyTag = true;
            Definition.familyTag = family;
        }

        return This();
    }

    internal TBuilder SetArmorProficiencyPrerequisite(ArmorCategoryDefinition category = null)
    {
        Definition.armorProficiencyPrerequisite = category != null;
        Definition.armorProficiencyCategory = category == null ? String.Empty : category.Name;
        return This();
    }

    #region Constructors

    protected FeatDefinitionBuilder(string name, Guid namespaceGuid) : base(name, namespaceGuid)
    {
    }

    protected FeatDefinitionBuilder(TDefinition original, string name, Guid namespaceGuid) : base(original, name,
        namespaceGuid)
    {
    }

    #endregion
}

[UsedImplicitly]
internal class FeatDefinitionBuilder : FeatDefinitionBuilder<FeatDefinition, FeatDefinitionBuilder>
{
    #region Constructors

    protected FeatDefinitionBuilder(string name, Guid namespaceGuid) : base(name, namespaceGuid)
    {
    }

    protected FeatDefinitionBuilder(FeatDefinition original, string name, Guid namespaceGuid) : base(original, name,
        namespaceGuid)
    {
    }

    #endregion
}
