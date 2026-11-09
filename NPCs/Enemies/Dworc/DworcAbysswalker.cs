using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.RadiantLifegem;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.NPCs.Enemies.Dworc
{
    class DworcAbysswalker : DworcCasterPuppet
    {
        protected override CasterKind Kind => CasterKind.Abysswalker;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.npcSlots = 5;
            NPC.knockBackResist = 0f;
            NPC.defense = 72;
            NPC.lifeMax = 3500;
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath31;
            NPC.value = 10000;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.AbysswalkerBanner>();
            // The old 1.2 NPC scale is replaced by the ready player-size equip rig; hitbox stays 18x40.
            // Poise/knockback are resolved last by GlobalNPC.PopulatePoiseProfiles.
        }
        //Spawns in the Jungle Underground and in the Cavern.
        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            // Jungle enemies stay out of the snow biome, even where it borders the jungle.
            if (player.ZoneSnow)
            {
                return 0f;
            }

            // these are all the regular stuff you get , now lets see......
            float chance = 0;

            //Ensuring it can't spawn if one already exists.
            int count = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == NPC.type)
                {
                    count++;
                    if (count > 0)
                    {
                        return 0;
                    }
                }
            }

            if ((player.ZoneMeteor || player.ZoneJungle) && tsorcRevampWorld.SuperHardMode && !player.ZoneDungeon && !(player.ZoneCorrupt || player.ZoneCrimson))
            {
                chance = 0.1f; // reduced from .15
            }
            if (player.ZoneDirtLayerHeight)
            {
                chance *= 1.3f;
            }
            if (player.ZoneRockLayerHeight)
            {
                chance *= 1.5f;
            }
            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
        }
        #endregion

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Voodoomaster Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Dworc Gore 3").Type, 1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.SuperHardmodeRule, ModContent.ItemType<FlameOfTheAbyss>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegemItem>(), 5));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 4));
        }

    }
}
