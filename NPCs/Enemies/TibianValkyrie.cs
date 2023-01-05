using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies
{
    class TibianValkyrie : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tibian Valkyrie");
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.Skeleton];
        }

        public override void SetDefaults()
        {
            AnimationType = NPCID.Skeleton;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 90;
            NPC.damage = 28;
            NPC.scale = 1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = .5f;
            NPC.value = 250;
            NPC.defense = 4;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.TibianValkyrieBanner>();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            int[] armorIDs = new int[] {
                ModContent.ItemType<Items.Armors.Magic.RedClothHat>(),
                ModContent.ItemType<Items.Armors.Magic.RedClothTunic>(),
                ModContent.ItemType<Items.Armors.Magic.RedClothPants>(),
            };
            npcLoot.Add(new DropMultiple(armorIDs, 30, 1, !NPC.downedBoss1));

            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.Torch, 10, 10, 20));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Ranged.Thrown.ThrowingSpear>(), 1, 20, 75));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Defensive.IronShield>(), 30));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Spears.OldHalberd>(), 30));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.Diamond, 5));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.DeadChicken>(), 10));

            //doesn't have a HM stats yet
            //if (Main.hardMode)
            //{
            //    npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.SoulofLight, 10));
            //}
        }

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

            if (spawnInfo.Player.townNPCs > 0f) return 0f;
            if (spawnInfo.Player.ZoneOverworldHeight && !Main.hardMode && !spawnInfo.Player.ZoneCrimson && !Main.dayTime) return 0.0427f;
            if (spawnInfo.Player.ZoneOverworldHeight && !Main.hardMode && !spawnInfo.Player.ZoneCrimson && Main.dayTime) return 0.038f;

            if (!Main.hardMode && !spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneJungle)
            {
                if (!spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && Main.dayTime) return 0.0433f;
                if (!spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && !Main.dayTime) return 0.0555f;
                if (spawnInfo.Player.ZoneDungeon && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight)) return 0.03857f;
            }
            return chance;
        }
        #endregion

        float boredTeleport = 0;
        float spearTimer = 0;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.65f, 0.05f, enragePercent: 0.5f, enrageTopSpeed: 2.4f); //0.7 was .11
            int damage = 10;
            if (!NPC.downedBoss1)
            {
                damage = 7;
            }
            tsorcRevampAIs.SimpleProjectile(NPC, ref spearTimer, 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), damage, 8, Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0), shootSound: SoundID.Item17);

            bool clearLineofSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);

            
            //If the enemy doesn't have line of sight for a good while, teleport far away from the player and try again. Then much later, they get one more chance to teleport.
            //Since this is an early enemy, the distance and time is not very aggressive.
            if (!clearLineofSight)
            {
                boredTeleport++;

                if (boredTeleport == 2600)
                {
                    tsorcRevampAIs.Teleport(NPC, 20, false);

                }

                if (boredTeleport == 5600)
                {
                    tsorcRevampAIs.Teleport(NPC, 30, false);
                    boredTeleport = 5601;
                }

            }
        }

        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {

            tsorcRevampAIs.RedKnightOnHit(NPC, true);
            if (Main.rand.NextBool(3))
            {
                spearTimer = 0;
            }
            
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {

            tsorcRevampAIs.RedKnightOnHit(NPC, projectile.DamageType == DamageClass.Melee);

            if (projectile.DamageType == DamageClass.Melee)
            {
                if (Main.rand.NextBool(3))
                {
                    spearTimer = 0;
                }
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTimer >= 150)
            {
                Texture2D spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/TibianValkyrie_Spear");
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 76, 58), drawColor, NPC.rotation, new Vector2(38, 34), NPC.scale, effects, 0);
                }
                else
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 76, 58), drawColor, NPC.rotation, new Vector2(38, 34), NPC.scale, effects, 0);
                }
            }
        }


        #region Gore
        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
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
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 3").Type, 1f);
                }
            }
        }
        #endregion
    }
}