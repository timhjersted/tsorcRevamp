using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    // Test 3 enemy — third iteration. Routes through SmartFighter3AI, which adds:
    //   - calibrated jump arc table (only proposes physically reachable jumps)
    //   - genuine action commitment (plan can't be invalidated mid-flight)
    //   - goal-aware A* heuristic (5× weight on vertical distance to player)
    // Same stats / texture / attack as TibianValkyrieSmart for direct A/B comparison.
    class TibianValkyrieSmart3 : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Enemies/TibianValkyrie";

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
            NPC.lifeMax = 90;
            NPC.damage = 22;
            NPC.scale = 1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = .5f;
            NPC.value = 0;
            NPC.defense = 4;

            int spearDamage = 10;
            if (Main.hardMode)
            {
                NPC.lifeMax = 260;
                NPC.damage = 40;
                NPC.defense = 12;
                spearDamage = 20;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 700;
                NPC.damage = 70;
                NPC.defense = 30;
                spearDamage = 35;
            }

            UsefulFunctions.AddAttack(NPC, 190, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), spearDamage, 8, shootSound: SoundID.Item17);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavigationTier = 0;
            globalNPC.MaxJumpPower = 9f;
            globalNPC.MaxJumpBoost = 5f;
            globalNPC.WeakTeleport = false;
            globalNPC.CanStopToFire = false;

            globalNPC.Aggression = 0.6f;
            globalNPC.Patience = 1.2f;
            globalNPC.Agility = 0.25f;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

        public override void AI()
        {
            SmartFighter3AI.Run(NPC, topSpeed: 1.55f, acceleration: 0.10f, attackRange: 700f);

            if (!NPC.downedBoss1)
            {
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackList[0].damage = 7;
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone) =>
            SmartFighter3AI.OnHit(NPC);

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone) =>
            SmartFighter3AI.OnHit(NPC);

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 140)
            {
                Texture2D spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/TibianValkyrie_Spear");
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition,
                    new Rectangle(NPC.frame.X, NPC.frame.Y, 76, 58), drawColor, NPC.rotation,
                    new Vector2(38, 34), NPC.scale, effects, 0);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0 && !Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 3").Type, 1f);
            }
        }
    }
}
