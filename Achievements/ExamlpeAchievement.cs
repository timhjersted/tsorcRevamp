using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Special;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Achievements;

// MinionBossKilled showcases an extremely simple ModAchievement.
// It is unlocked when MinionBossBody is defeated.

#region Slayer
public class SlayerLeonhard : ModAchievement
{
    // It is possible to share a texture and use Index to choose which icon is used from a texture.
    public override string TextureName => "tsorcRevamp/Achievements/TestAllAchievements";
    public override int Index => 0;
    public override void SetStaticDefaults()
    {
        Achievement.SetCategory(AchievementCategory.Slayer); //Slayer, Collector, Explorer or Challenger
        AddNPCKilledCondition(ModContent.NPCType<LeonhardPhase1>());
    }

    // By default a ModAchievement will be placed at the end of the achievement ordering.
    // GetDefaultPosition is used to position a ModAchievement in relation to vanilla achievements.
    public override Position GetDefaultPosition() => new After("EYE_ON_YOU");

    // We can use GetModdedConstraints to dictate the relative ordering of ModAchievements.
    /* public override IEnumerable<Position> GetModdedConstraints() {
        yield return new After(ModContent.GetInstance<M>());
    } */

    public override void OnCompleted(Achievement achievement)
    {
        // Make Red fireworks
        int fireworkProjectile = ProjectileID.RocketFireworksBoxRed + Main.rand.Next(4);
        Projectile.NewProjectile(Main.LocalPlayer.GetSource_FromThis(), Main.LocalPlayer.Top, new Vector2(Main.rand.NextFloat(-2, 2), -Main.rand.NextFloat(2f, 4f)).RotatedByRandom(0.3f), fireworkProjectile, 0, 0, Main.myPlayer);
    }
}
#endregion

#region Explorer

public class ExploreVillage : ModAchievement
{
    // It is possible to share a texture and use Index to choose which icon is used from a texture.
    public override string TextureName => "tsorcRevamp/Achievements/TestAllAchievements";
    public override int Index => 1;
    public CustomFlagCondition ExploreVillageCondition { get; private set; }
    public override void SetStaticDefaults()
    {
        Achievement.SetCategory(AchievementCategory.Explorer); //Slayer, Collector, Explorer or Challenger
        ExploreVillageCondition = AddCondition();
    }

    // By default a ModAchievement will be placed at the end of the achievement ordering.
    // GetDefaultPosition is used to position a ModAchievement in relation to vanilla achievements.
    // public override Position GetDefaultPosition() => new After("EYE_ON_YOU");

    // We can use GetModdedConstraints to dictate the relative ordering of ModAchievements.
    public override IEnumerable<Position> GetModdedConstraints()
    {
        yield return new After(ModContent.GetInstance<SlayerLeonhard>());
    }

    public override void OnCompleted(Achievement achievement)
    {
        // Make Yellow fireworks
        int fireworkProjectile = ProjectileID.RocketFireworksBoxYellow + Main.rand.Next(4);
        Projectile.NewProjectile(Main.LocalPlayer.GetSource_FromThis(), Main.LocalPlayer.Top, new Vector2(Main.rand.NextFloat(-2, 2), -Main.rand.NextFloat(2f, 4f)).RotatedByRandom(0.3f), fireworkProjectile, 0, 0, Main.myPlayer);
    }
}
#endregion

#region Collector

public class CollectorCosmicWatch : ModAchievement
{
    // It is possible to share a texture and use Index to choose which icon is used from a texture.
    public override string TextureName => "tsorcRevamp/Achievements/Template";
    public override void SetStaticDefaults()
    {
        Achievement.SetCategory(AchievementCategory.Collector); //Slayer, Collector, Explorer or Challenger
        AddItemCraftCondition(ModContent.ItemType<CosmicWatch>());
    }

    // We can use GetModdedConstraints to dictate the relative ordering of ModAchievements.
    public override IEnumerable<Position> GetModdedConstraints()
    {
        yield return new After(ModContent.GetInstance<ExploreVillage>());
    }

    public override void OnCompleted(Achievement achievement)
    {
        // Make Green fireworks
        int fireworkProjectile = ProjectileID.RocketFireworksBoxGreen + Main.rand.Next(4);
        Projectile.NewProjectile(Main.LocalPlayer.GetSource_FromThis(), Main.LocalPlayer.Top, new Vector2(Main.rand.NextFloat(-2, 2), -Main.rand.NextFloat(2f, 4f)).RotatedByRandom(0.3f), fireworkProjectile, 0, 0, Main.myPlayer);
    }
}
#endregion

#region Challenger
public class ChallengerEternalCrystalAchievement : ModAchievement
{
    // It is possible to share a texture and use Index to choose which icon is used from a texture.
    public override string TextureName => "tsorcRevamp/Achievements/Template";

    public CustomFlagCondition EternalCrystalHitCondition { get; private set; }
    public CustomIntCondition TotalDamageCondition { get; private set; }
    public CustomIntCondition KillCosmicCrystalLizardCondition { get; private set; }

    // This achievement is "hidden", meaning its name and description will both appear as "???" in the achievements menu, until at least 1 ExampleWormHead has been killed.
    public override bool Hidden => TotalDamageCondition.Value == 0;

    public override void SetStaticDefaults()
    {
        Achievement.SetCategory(AchievementCategory.Challenger);

        // Cosmic Crystal Lizard Hit once with Melee
        EternalCrystalHitCondition = AddCondition();

        // 15 total damage dealt to Cosmic Crystal Lizard
        TotalDamageCondition = AddIntCondition("TotalDamage", 15);

        // 5 Cosmic Crystal Lizards killed
        KillCosmicCrystalLizardCondition = AddIntCondition("LizardCount", 5);
        AchievementsHelper.OnNPCKilled += NPCKilledListener;

        var EternalCrystalPickupCondition = AddItemPickupCondition(ModContent.ItemType<EternalCrystal>());
        EternalCrystalPickupCondition.OnComplete += (condition) => Main.NewText(LangUtils.GetTextValue("Achievements.ChallengerEternalCrystalAchievement.Progress", Achievement.FriendlyName));

        // By default, a ModAchievement will have a suitable IAchievementTracker assigned. A ModAchievement with multiple conditions like this one will automatically use the ConditionsCompletedTracker as its IAchievementTracker. ConditionsCompletedTracker will simply count how many conditions out of the total are completed and report that as the progress.
        // Rather than that, we are using a custom IAchievementTracker that calculates the progress by weighting each condition to demonstrate how to implement and use a custom IAchievementTracker.
        Achievement.UseTracker(new CustomTracker(
            new Dictionary<AchievementCondition, float>()
            {
                [EternalCrystalHitCondition] = 1.5f,
                [TotalDamageCondition] = 3f,
                [KillCosmicCrystalLizardCondition] = 4f,
                [EternalCrystalPickupCondition] = 2f,
            }
        ));
    }

    private void NPCKilledListener(Player player, short npcId)
    {
        if (player.whoAmI != Main.myPlayer)
        {
            return;
        }

        if (npcId == ModContent.NPCType<CosmicCrystalLizard>())
        {
            // Int conditions will automatically complete once you've incremented it enough. There is no need to call the Complete method manually.
            KillCosmicCrystalLizardCondition.Value++;
        }
    }
    public override void OnCompleted(Achievement achievement)
    {
        // Make Yellow fireworks
        int fireworkProjectile = ProjectileID.RocketFireworksBoxYellow + Main.rand.Next(4);
        Projectile.NewProjectile(Main.LocalPlayer.GetSource_FromThis(), Main.LocalPlayer.Top, new Vector2(Main.rand.NextFloat(-2, 2), -Main.rand.NextFloat(2f, 4f)).RotatedByRandom(0.3f), fireworkProjectile, 0, 0, Main.myPlayer);
    }
}
#endregion

public class CustomTracker : ConditionFloatTracker
{
	private Dictionary<AchievementCondition, float> weightedConditions;

	public CustomTracker(Dictionary<AchievementCondition, float> weightedConditions) {
		this.weightedConditions = weightedConditions;
		foreach ((AchievementCondition condition, float weight) in weightedConditions) {
			_maxValue += weight;
			condition.OnComplete += OnConditionCompleted;
		}
	}

	private void OnConditionCompleted(AchievementCondition condition) {
		SetValue(Math.Min(_value + weightedConditions[condition], _maxValue));
	}

	protected override void Load() {
		foreach ((AchievementCondition condition, float weight) in weightedConditions) {
			if (condition.IsCompleted)
				_value += weight;
		}
	}
}