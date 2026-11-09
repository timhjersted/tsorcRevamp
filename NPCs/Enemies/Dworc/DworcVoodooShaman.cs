using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.ConsumableSoul;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.RadiantLifegem;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies.Dworc
{
    public class DworcVoodooShaman : DworcCasterPuppet
    {
        protected override CasterKind Kind => CasterKind.Shaman;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.npcSlots = 3;
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath31;
            NPC.lifeMax = 750;
            NPC.defense = 28;
            NPC.value = 7500;
            NPC.knockBackResist = 0.2f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DworcVoodooShamanBanner>();
            // Poise/knockback are resolved last by GlobalNPC.PopulatePoiseProfiles.
        }
        //yes i tweaked the drop rates. Fight Me
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 31, 1, 4));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 31, 1, 3));
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 31, 1, 5));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 32));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 35));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 40));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 26));
            npcLoot.Add(ItemDropRule.Common(ItemID.FlaskofNanites, 5));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 26));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 24));        
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 3, 5));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloodredMossClump>(), 3, 3, 6));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FadingSoul>(), 5));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CharcoalPineResin>(), 4));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegemItem>(), 3));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 8));
            npcLoot.Add(ItemDropRule.Common(ItemID.SoulofLight, 2, 1, 2));
        }

        //Spawns in the Jungle and in the Cavern in HM.

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Voodoomaster Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                }
            }
        }
    }
}
