using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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
            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.TibianValkyrieBanner>();
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Torch);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Ranged.ThrowingSpear>(), Main.rand.Next(20, 76));
            if (Main.rand.Next(10) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.IronShield>(), 1, false, -1);
            if (Main.rand.Next(10) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.OldHalberd>(), 1, false, -1);
            if (Main.rand.Next(20) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.OldDoubleAxe>(), 1, false, -1);
            if (Main.rand.Next(2) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Diamond);
            if (Main.rand.Next(20) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DeadChicken>());
        }


        #region Spawn

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;

            if (spawnInfo.Invasion)
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

        float spearTimer = 0;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.65f, 0.11f, enragePercent: 0.5f, enrageTopSpeed: 2.4f);
            tsorcRevampAIs.SimpleProjectile(NPC, ref spearTimer, 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), 10, 8, Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0), soundType: 2, soundStyle: 17);

            if (NPC.justHit)
            {
                spearTimer = 150;
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTimer >= 150)
            {
                Texture2D spearTexture = Mod.GetTexture("NPCs/Enemies/TibianValkyrie_Spear");
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

                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Tibian Valkyrie Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Tibian Valkyrie Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Tibian Valkyrie Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Tibian Valkyrie Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Tibian Valkyrie Gore 3").Type, 1f);
            }
        }
        #endregion
    }
}