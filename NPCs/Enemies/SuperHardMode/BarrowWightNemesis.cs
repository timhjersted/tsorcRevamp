using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    public class BarrowWightNemesis : BarrowWightBase
    {
        protected override bool IsNemesis => true;
        protected override string InvaderTitle => "Barrow Wight Nemesis";
        protected override float DriftSpeed => 3.4f;
        protected override float LungeDistanceCap => 360f;
        protected override int CutExpertDamage => 116;
        // Temporary outfit until Nemesis armor exists; spectral rendering preserves these shaders.
        protected override int HeadArmorDyeItemType => ItemID.BrownDye;
        protected override int BodyArmorDyeItemType => ItemID.BrownDye;
        protected override int LegsArmorDyeItemType => ItemID.BrownDye;

        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.npcSlots = 5;
            NPC.defense = 40;
            NPC.lifeMax = 24000; // EnemyBalance applies SHM progression scaling.
            NPC.lavaImmune = true;
            NPC.value = 10000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BarrowWightNemesisBanner>();
        }

        public override void OnHumanoidMeleeHit(Player target)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.BrokenArmor, 10 * 60);
                target.AddBuff(BuffID.Frostburn, 10 * 60);
                target.AddBuff(BuffID.Chilled, 10 * 60);
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 600 * 60);
                target.AddBuff(ModContent.BuffType<PowerfulCurseBuildup>(), 600 * 60);
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            float chance = 0;
            if (tsorcRevampWorld.SuperHardMode)
            {
                if (player.ZoneSkyHeight || spawnInfo.Player.ZoneDungeon || spawnInfo.Player.ZoneSnow)
                {
                    chance = 0.17f;
                }
                else if (player.ZoneOverworldHeight && !Main.dayTime)
                {
                    chance = 0.02f;
                }
            }

            if (!Main.dayTime)
            {
                chance *= 2;
            }
            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CursedSoul>(), 1, 3, 3));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PurgingStone>(), 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BarrowBlade>(), 5));
        }
    }
}
