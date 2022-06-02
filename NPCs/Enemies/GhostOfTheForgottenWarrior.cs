using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class GhostOfTheForgottenWarrior : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.damage = 35;
            NPC.lifeMax = 95;
            NPC.defense = 16;
            NPC.value = 350;
            NPC.width = 20;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.knockBackResist = 0.1f;
            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.GhostOfTheForgottenWarriorBanner>();

            AnimationType = NPCID.GoblinWarrior;
            Main.npcFrameCount[NPC.type] = 16;

            if (Main.hardMode)
            {
                NPC.lifeMax = 195;
                NPC.defense = 20;
                NPC.value = 450;
                NPC.damage = 50;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1095;
                NPC.defense = 70;
                NPC.damage = 100;
                NPC.value = 1000;
            }
        }
        public override void OnKill()
        {
            if (Main.rand.Next(10) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Ranged.EphemeralThrowingSpear>(), Main.rand.Next(15, 26));
            if (Main.rand.Next(10) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.WallTome>());
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;

            if (!Main.hardMode && NPC.downedBoss3 && spawnInfo.Player.ZoneDungeon)
            {
                return 0.25f;
            }
            else if (Main.hardMode && spawnInfo.Player.ZoneDungeon)
            {
                return 0.12f;
            }
            else if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon)
            {
                return 0.1f; //.05 is 3.85%
            }

            return chance;
        }

        #endregion


        float spearTimer = 0;

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.5f, .04f, 0.2f, true, enragePercent: 0.2f, enrageTopSpeed: 2.4f);

            bool canFire = NPC.Distance(Main.player[NPC.target].Center) < 1600 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0);
            tsorcRevampAIs.SimpleProjectile(NPC, ref spearTimer, 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), 20, 8, canFire, true, 2, 17);

            if (NPC.justHit)
            {
                spearTimer = 0f;
            }
        }

        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null)
            {
                spearTexture = Mod.GetTexture("Projectiles/Enemy/BlackKnightGhostSpear");
            }
            if (spearTimer >= 150)
            {
                Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 0.3f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.Next(3) == 1)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y);
                }
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 38), NPC.scale, effects, 0); // facing left (8, 38 work)
                }
                else
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 38), NPC.scale, effects, 0); // facing right, first value is height, higher number is higher
                }
            }
        }
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

                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Wild Warrior Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Wild Warrior Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Wild Warrior Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Wild Warrior Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Wild Warrior Gore 3").Type, 1f);
                if (Main.rand.Next(10) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GoldenKey, 1);
            }
        }
    }
}