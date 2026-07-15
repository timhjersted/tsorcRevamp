using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Spawned by ArtoriasAbyssBlast: fans out in a straight line, gets a brief homing window partway
    // through its life, then keeps whatever heading it ends up with for the remainder before
    // dissipating. Reuses AbyssLurkerFlameOrb's sprite/visual identity (same purple orb).
    class ArtoriasFlameOrb : ModProjectile
    {
        const int FanOutTicks = 3 * 60;
        const int HomingTicks = 60;
        const int FinalTicks = 3 * 60;
        const int TotalTicks = FanOutTicks + HomingTicks + FinalTicks;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/AbyssLurkerFlameOrb";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.scale = 1.1f;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.9f;
            Projectile.timeLeft = TotalTicks;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.1f);
            Animate();

            Projectile.ai[0]++;
            float elapsed = Projectile.ai[0];

            // Homing window only - straight fan-out before it, straight (whatever heading it ends
            // up with) after it.
            if (elapsed > FanOutTicks && elapsed <= FanOutTicks + HomingTicks)
            {
                Player target = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
                if (target.active && !target.dead)
                {
                    Vector2 desired = Projectile.DirectionTo(target.Center) * Projectile.velocity.Length();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.06f);
                }
            }

            FaceVelocity();

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch,
                    Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.4f);
                Main.dust[dust].noGravity = true;
            }
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
        }

        void FaceVelocity()
        {
            if (Projectile.velocity.LengthSquared() > 0.01f)
            {
                Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
                Projectile.rotation = MathHelper.PiOver2;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<AbyssInferno>(), 4 * 60, false);
            SpawnFlameDust(25, 4.6f, 1.9f);

            // ai[1] is the firing NPC's whoAmI, offset by +1 (0 = "no owner" - ArtoriasAbyssBlast's
            // own orb fan spawns this projectile anonymously) so the dodge-punish-chain system knows
            // an Abyss Slash orb finisher actually connected.
            int ownerIdx = (int)Projectile.ai[1] - 1;
            if (ownerIdx >= 0 && ownerIdx < Main.maxNPCs && Main.npc[ownerIdx].active
                && Main.npc[ownerIdx].ModNPC is PuppetNPC invader)
            {
                invader.ReportAttackHit();
            }
        }

        public override void OnKill(int timeLeft)
        {
            SpawnFlameDust(15, 3.2f, 1.7f);
        }

        void SpawnFlameDust(int count, float speed, float scale)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(speed, speed);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch,
                    velocity.X, velocity.Y, 80, new Color(255, 135, 35), scale);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
