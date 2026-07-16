using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Physical axe used by StuddedLeatherWarrior's throw-and-retrieve combo.
    /// ai[0] = embedded flag, ai[1] = owning puppet NPC index, ai[2] = already hit a player.
    /// </summary>
    public class PuppetThrownDualBladedAxe : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Axes/DualBladedAxe";

        private bool Embedded => Projectile.ai[0] == 1f;
        private int OwnerNpcIndex => (int)Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.scale = 0.85f;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = 360;
        }

        public override bool? CanDamage() => Embedded || Projectile.ai[2] == 1f ? false : null;

        public override void AI()
        {
            if (OwnerNpcIndex < 0 || OwnerNpcIndex >= Main.maxNPCs
                || !Main.npc[OwnerNpcIndex].active)
            {
                Projectile.Kill();
                return;
            }

            if (Embedded)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.hostile = false;
                if (Main.rand.NextBool(18))
                {
                    int dust = Dust.NewDust(Projectile.Bottom - new Vector2(5f, 3f), 10, 5,
                        DustID.Dirt, 0f, -0.4f, 100, default, 0.75f);
                    Main.dust[dust].velocity.X *= 0.3f;
                }
                return;
            }

            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.18f, 14f);
            float spinDirection = Projectile.velocity.X < 0f ? -1f : 1f;
            Projectile.rotation += 0.32f * spinDirection;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // The thrown axe may pass through the target on its way to the floor, but can only
            // damage once. It must remain alive so the puppet still has something to retrieve.
            Projectile.ai[2] = 1f;
            Projectile.netUpdate = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool hitGroundWhileFalling = oldVelocity.Y > 0f && Projectile.velocity.Y != oldVelocity.Y;
            if (!hitGroundWhileFalling)
            {
                // Walls and ceilings deflect the cast, but only a downward floor collision plants
                // it. This keeps the retrieval target on the ground as the telegraph promises.
                if (Projectile.velocity.X != oldVelocity.X)
                    Projectile.velocity.X = -oldVelocity.X * 0.25f;
                if (Projectile.velocity.Y != oldVelocity.Y)
                    Projectile.velocity.Y = Math.Abs(oldVelocity.Y) * 0.25f;
                return false;
            }

            Projectile.ai[0] = 1f;
            Projectile.hostile = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.rotation = oldVelocity.X < 0f
                ? -MathHelper.PiOver4
                : MathHelper.PiOver4;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.7f, Pitch = -0.1f }, Projectile.Center);
            Collision.HitTiles(Projectile.position, oldVelocity, Projectile.width, Projectile.height);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(
                    Projectile.Bottom - new Vector2(Projectile.width * 0.5f, 4f),
                    Projectile.width,
                    8,
                    DustID.Dirt,
                    oldVelocity.X * 0.12f + Main.rand.NextFloat(-1.5f, 1.5f),
                    Main.rand.NextFloat(-2.5f, -0.4f),
                    80);
            }
            return false;
        }
    }
}
