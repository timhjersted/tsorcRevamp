using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///OolacileSerpent's venom spit. Copied from ShadowShot (6-frame animation) with purple "boiling poison"
    ///travel dust. ai[0] = gravity per tick (0 for straight spray shots, ~0.25 for lobbed shots).
    ///On death (tile or player hit) it splashes and leaves an AcidPool on the ground below.
    ///</summary>
    class VenomSpit : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.height = Projectile.width = 15;
            Projectile.tileCollide = true;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.light = 0.5f;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            Animate();

            //Lobbed shots arc under their own gravity
            Projectile.velocity.Y += Projectile.ai[0];
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }
            if (Projectile.ai[0] > 0f)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            //Hot boiling poison: bright purple core + venom bubbles drifting off it
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.AncientLight, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 150, Color.Purple, 0.9f);
            Main.dust[dust].noGravity = true;
            if (Main.rand.NextBool(2))
            {
                int bubble = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Venom, 0f, -0.5f, 120, default, 1.1f);
                Main.dust[bubble].noGravity = true;
                Main.dust[bubble].velocity *= 0.3f;
            }
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Venom, 3 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath26 with { Volume = 0.25f, PitchVariance = 0.2f }, Projectile.Center); //acid splash

            //Purple splash burst
            for (int i = 0; i < 18; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.AncientLight, vel.X, vel.Y, 120, Color.Purple, 1.3f);
                Main.dust[dust].noGravity = true;
                int bubble = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Venom, vel.X, vel.Y - 1f, 100, default, 1.2f);
                Main.dust[bubble].noGravity = Main.rand.NextBool();
            }

            //Leave an acid pool on the ground surface below the impact (if any is close enough)
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int tileX = (int)(Projectile.Center.X / 16f);
                int tileY = (int)(Projectile.Center.Y / 16f);
                for (int d = 0; d < 6; d++)
                {
                    int y = tileY + d;
                    if (y >= Main.maxTilesY)
                    {
                        break;
                    }
                    Tile tile = Main.tile[tileX, y];
                    if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                    {
                        Vector2 poolPos = new Vector2(tileX * 16f + 8f - AcidPool.PoolWidth / 2f, y * 16f - AcidPool.PoolHeight);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), poolPos + new Vector2(AcidPool.PoolWidth / 2f, AcidPool.PoolHeight / 2f), Vector2.Zero, ModContent.ProjectileType<AcidPool>(), 10, 0f, Main.myPlayer);
                        break;
                    }
                }
            }
        }
    }
}
