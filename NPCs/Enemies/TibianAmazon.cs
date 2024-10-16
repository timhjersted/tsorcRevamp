﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies
{
    class TibianAmazon : ModNPC
    {
        public int throwingKnifeDamage = 8;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.Skeleton];
        }
        public override void SetDefaults()
        {
            AnimationType = NPCID.Skeleton;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 85;
            NPC.damage = 20;
            NPC.scale = 1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = .6f;
            NPC.value = 430;
            NPC.defense = 2;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.TibianAmazonBanner>();

            if (!NPC.downedBoss1)
            {
                throwingKnifeDamage = 6;
            }

            if (Main.hardMode)
            {
                NPC.lifeMax = 180;
                NPC.defense = 16;
                NPC.value = 900;
                NPC.damage = 50;
                throwingKnifeDamage = 20;
            }
            UsefulFunctions.AddAttack(NPC, 160, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnife>(), throwingKnifeDamage, 8, shootSound: SoundID.Item17);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            int[] armorIDs = new int[] {
                ModContent.ItemType<Items.Armors.Magic.RedClothHat>(),
                ModContent.ItemType<Items.Armors.Magic.RedClothTunic>(),
                ModContent.ItemType<Items.Armors.Magic.RedClothPants>(),
            };
            npcLoot.Add(new DropMultiple(armorIDs, 30, 1, !NPC.downedBoss1));

            npcLoot.Add(ItemDropRule.Common(ItemID.Torch, 20, 10, 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.ThrowingKnife, 1, 20, 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DeadChicken>(), 25));
        }


        //Spawns on the Surface and into the Underground. Does not spawn in the Dungeon, Hardmode, Meteor, or if there are Town NPCs.

        #region Spawn

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            Player p = spawnInfo.Player;
            if (spawnInfo.Invasion || Sky(p) || spawnInfo.Player.ZoneSnow)
            {
                chance = 0;
                return chance;
            }

            if (spawnInfo.Player.townNPCs > 0f || tsorcRevampWorld.SuperHardMode || spawnInfo.Player.ZoneDungeon) chance = 0f;
            if (!tsorcRevampWorld.SuperHardMode && (spawnInfo.Player.ZoneOverworldHeight || spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight))
            {
                if (!(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return 0.05f;
                if (!(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && !Main.dayTime) return 0.055f;
                if (!(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && Main.dayTime) return 0.0534f;
                if (spawnInfo.Player.ZoneMeteor && !Main.dayTime) return 0.0725f;
            }

            return chance;
        }
        #endregion

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.8f, 0.15f, enragePercent: 0.2f, enrageTopSpeed: 2.2f, canPounce: false);
        }


        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.FighterOnHit(NPC, true);
            if (Main.rand.NextBool(3))
            {
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 0;
                NPC.netUpdate = true;
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.FighterOnHit(NPC, projectile.DamageType == DamageClass.Melee);

            if (projectile.DamageType == DamageClass.Melee)
            {
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 0;
                NPC.netUpdate = true;
            }
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 1);
        }

        public static Texture2D knifeTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimerCap * 3f / 4f)
            {
                float rotation = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1).ToRotation() + MathHelper.PiOver2;
                SpriteEffects effects = NPC.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                UsefulFunctions.EnsureLoaded(ref knifeTexture, "tsorcRevamp/NPCs/Enemies/TibianAmazon_Knife");
                spriteBatch.Draw(knifeTexture, NPC.Center - Main.screenPosition - new Vector2(0, NPC.gfxOffY), new Rectangle(NPC.frame.X, NPC.frame.Y, 60, 56), drawColor, rotation, new Vector2(30, 32), NPC.scale, effects, 0);
            }
        }

        #region Gore
        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 25; i++)
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
                for (int i = 0; i < 4; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }

                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Amazon Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Amazon Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Amazon Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Amazon Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Amazon Gore 3").Type, 1f);
                }
            }
        }
        #endregion
    }
}