using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Hostile counterpart to the vanilla Flaming Arrow (ProjectileID.FlamingArrow, 82). Written for
    /// Owl Father's Bow of Earendil volley — puppet ranged attacks never fire the backing vanilla
    /// weapon's real projectile, so a hostile stand-in is required.
    ///
    /// Same shape as EnemyShadowflameArrow (aiStyle 1 / WoodenArrowFriendly AIType) with the vanilla
    /// arrow's own sprite and the On Fire debuff on hit, matching vanilla FlamingArrow's own effect.
    /// </summary>
    public class EnemyFlamingArrow : ModProjectile
    {
        // Intentional permanent texture reuse of the vanilla flaming arrow sprite.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FlamingArrow;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.aiStyle = 1;
            AIType = ProjectileID.WoodenArrowFriendly;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.55f, 0.28f, 0.06f);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch,
                    Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.1f);
                dust.noGravity = true;
                dust.velocity *= 0.4f;
            }
        }

        /// <summary>ai[2] value for the vanilla hostile Flaming Arrow burn (1-in-3 chance, 7s), used by the Man Hunter.
        /// The default 0 keeps Owl Father's guaranteed 4s burn. ai[0] is not free: the vanilla arrow AI (aiStyle 1)
        /// counts its gravity delay in it.</summary>
        public const float VanillaBurnMode = 1f;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Vanilla Projectile.StatusPlayer: a hostile type-82 arrow sets On Fire 420t on a 1-in-3 roll.
            // OnHitPlayer runs on the hit player's machine, so the roll is local to the victim, as in vanilla.
            if (Projectile.ai[2] == VanillaBurnMode)
            {
                if (Main.rand.NextBool(3))
                {
                    target.AddBuff(BuffID.OnFire, 420);
                }

                return;
            }

            target.AddBuff(BuffID.OnFire, 4 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            // A modded projectile does NOT inherit vanilla's hardcoded type-specific tile-hit puff, so
            // without this the arrow vanishes silently on hitting a tile (same note as EnemyShadowflameArrow).
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch,
                    Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.2f);
                dust.noGravity = true;
            }

            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
        }
    }
}
