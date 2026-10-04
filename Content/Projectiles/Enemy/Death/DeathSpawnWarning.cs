using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace tsorcRevamp.Content.Projectiles.Enemy.Death
{
    /// <summary>
    /// Non-damaging spawn warning for Death's larger projectiles. It gathers dust at the
    /// exact spawn point, then creates the configured projectile and transfers its damage.
    /// </summary>
    class DeathSpawnWarning : ModProjectile
    {
        public const int FlamingScytheWarningTicks = 24;
        const int ReleaseSoundFlag = 1000;

        int WarningTicks => Math.Max(1, (int)Projectile.ai[2] % ReleaseSoundFlag);
        bool PlayReleaseSound => Projectile.ai[2] >= ReleaseSoundFlag;
        float StoredColor => Projectile.ai[1];

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
            Projectile.hide = true;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void AI()
        {
            int age = Math.Max(0, WarningTicks - (Projectile.timeLeft - 2));
            SpawnWarningDust(age);

            if (age < WarningTicks)
            {
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnWarnedProjectile();
            }

            Projectile.Kill();
        }

        void SpawnWarningDust(int age)
        {
            if (Main.dedServ)
            {
                return;
            }

            const float radius = 26f;
            const int count = 2;
            Color baseColor = UsefulFunctions.ColorFromFloat(StoredColor);
            Color tint = Color.Lerp(baseColor, new Color(42, 8, 60), 0.62f);
            int dustType = DustID.Torch;

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Vector2 position = Projectile.Center + offset;
                Vector2 inward = (Projectile.Center - position).SafeNormalize(Vector2.Zero);
                Dust dust = Dust.NewDustPerfect(
                    position,
                    dustType,
                    inward * Main.rand.NextFloat(0.7f, 1.5f),
                    140,
                    tint,
                    Main.rand.NextFloat(0.5f, 0.8f));
                dust.noGravity = true;
            }

            if (age >= WarningTicks - 4)
            {
                Dust glint = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.AncientLight,
                    Main.rand.NextVector2Circular(1f, 1f),
                    115,
                    tint * 0.8f,
                    Main.rand.NextFloat(0.35f, 0.55f));
                glint.noGravity = true;
            }
        }

        void SpawnWarnedProjectile()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectileDirect(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                direction,
                ModContent.ProjectileType<DeathFlamingScythe>(),
                Projectile.damage,
                0f,
                Main.myPlayer,
                Projectile.Center.X,
                Projectile.Center.Y);

            if (PlayReleaseSound)
            {
                Item sickle = new Item();
                sickle.SetDefaults(ItemID.DeathSickle);
                if (sickle.UseSound.HasValue)
                {
                    SoundEngine.PlaySound(sickle.UseSound.Value, Projectile.Center);
                }
            }
        }

        public static int SpawnFlamingScythe(
            IEntitySource source,
            Vector2 position,
            Vector2 direction,
            int damage,
            float color,
            bool playReleaseSound,
            int warningTicks = FlamingScytheWarningTicks)
        {
            return Spawn(
                source,
                position,
                direction,
                damage,
                color,
                warningTicks,
                playReleaseSound);
        }

        static int Spawn(
            IEntitySource source,
            Vector2 position,
            Vector2 velocity,
            int damage,
            float color,
            int warningTicks,
            bool playReleaseSound)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return -1;
            }

            float packedWarningTicks = Math.Clamp(warningTicks, 1, ReleaseSoundFlag - 1);
            if (playReleaseSound)
            {
                packedWarningTicks += ReleaseSoundFlag;
            }

            int index = Projectile.NewProjectile(
                source,
                position,
                velocity,
                ModContent.ProjectileType<DeathSpawnWarning>(),
                damage,
                0f,
                Main.myPlayer,
                ModContent.ProjectileType<DeathFlamingScythe>(),
                color,
                packedWarningTicks);

            if (index >= 0 && index < Main.maxProjectiles)
            {
                Main.projectile[index].damage = damage;
                Main.projectile[index].timeLeft = Math.Clamp(warningTicks, 1, ReleaseSoundFlag - 1) + 2;
                Main.projectile[index].netUpdate = true;
            }

            return index;
        }
    }
}
