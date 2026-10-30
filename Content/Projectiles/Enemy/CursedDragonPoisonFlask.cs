using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// The Cursed Dragon's Poison Cloud payload: a green flask lobbed from the spear tip on a solved ballistic arc.
    /// It shatters on the first tile, on touching a player, or at the end of its flight, and the server spawns
    /// <see cref="CursedDragonPoisonCloud"/> where it broke. The flask itself does no damage.
    /// </summary>
    public class CursedDragonPoisonFlask : ModProjectile
    {
        // Deliberate, permanent share of vanilla's Toxic Flask sprite (a green glass flask), not a placeholder.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ToxicFlask;

        /// <summary>Gravity per tick. The caller solves its launch velocity against this same value.</summary>
        public const float Gravity = 0.3f;

        // A flask that never meets a tile (fell into a pit) still bursts, after 2s.
        private const int MaxFlightTicks = 120;
        private const float PlayerShatterRange = 28f;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = MaxFlightTicks;
            Projectile.aiStyle = -1;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.velocity.Y += Gravity;

            // Tumbles end over end in the direction it was thrown.
            Projectile.rotation += 0.25f * System.Math.Sign(Projectile.velocity.X);

            // Sparse green drip with the plum curse accent, so the arc is readable against any background.
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust drip = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, -Projectile.velocity * 0.15f, 0, default, 1.2f);
                drip.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(6))
            {
                PowerfulCurseBuildup.SpawnAccentDust(Projectile.Center, 0, 0, -Projectile.velocity * 0.1f, 1f);
            }

            Lighting.AddLight(Projectile.Center, 0.1f, 0.3f, 0.08f);

            // Shatters on whoever it reaches first. Every peer runs this on the same synced positions, and only the
            // server's OnKill spawns the cloud.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }

                float distance = Vector2.Distance(player.Center, Projectile.Center);
                if (distance <= PlayerShatterRange)
                {
                    Projectile.Kill();
                    return;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.8f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item107 with { Volume = 0.6f, Pitch = -0.2f }, Projectile.Center);

                for (int i = 0; i < 70; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f);
                    Dust burst = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, velocity, 0, default, Main.rand.NextFloat(1.2f, 2f));
                    burst.noGravity = true;
                }

                for (int i = 0; i < 12; i++)
                {
                    PowerfulCurseBuildup.SpawnAccentDust(Projectile.Center, 0, 0, Main.rand.NextVector2Circular(3f, 3f), 1.4f);
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CursedDragonPoisonCloud>(), 0, 0f, Main.myPlayer);
        }
    }
}
