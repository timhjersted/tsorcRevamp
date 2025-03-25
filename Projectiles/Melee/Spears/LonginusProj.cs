using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Spears;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class LonginusProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 85f;
        public override float HoldoutRangeMax => 265f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.Fireworks;
        bool hasHealed = false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            if (Main.rand.NextBool(6) && !hasHealed)
            {
                player.statLife += Longinus.HealOnHit;
                player.HealEffect(Longinus.HealOnHit, true);
                hasHealed = true;
            }

            // Spawn un seul RubyBolt au toucher, inspiré de Starfall
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 boltSpawnPos = target.Center;
                boltSpawnPos.Y -= 500f; // 600 pixels au-dessus
                boltSpawnPos.X += Main.rand.Next(-200, 201); // Dispersion horizontale aléatoire (±200 pixels)

                float velX = target.Center.X - boltSpawnPos.X + Main.rand.Next(-40, 41) * 0.03f;
                float velY = target.Center.Y - boltSpawnPos.Y;
                if (velY < 0f)
                {
                    velY *= -1f;
                }
                if (velY < 20f)
                {
                    velY = 20f;
                }

                Vector2 boltVelocity = Vector2.Normalize(new Vector2(velX, velY)) * 10f;
                boltVelocity.Y += Main.rand.Next(-40, 41) * 0.02f;

                int boltDamage = damageDone / 2;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    boltSpawnPos,
                    boltVelocity * 1.8f,
                    ProjectileID.RubyBolt,
                    boltDamage,
                    3f,
                    Projectile.owner
                );
            }
        }
    }
}
