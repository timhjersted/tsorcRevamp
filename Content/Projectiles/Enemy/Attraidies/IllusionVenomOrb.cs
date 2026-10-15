using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Attraidies
{
    // Attraidies Illusion's Venom Orb: a lobbed poison globe that splashes into an IllusionVenomField where it lands.
    // Art: 16x16 single frame, round, no leading edge, so it spins instead of facing its velocity.
    // Birth: forms at the staff tip during the caster's 50t tell (preview drawn by AttraidiesIllusion.PreDraw).
    // Life: ballistic, Gravity px/t^2 per tick; the caster solves the launch so it lands 64px past the player.
    // Death: tile or player contact bursts and spawns the field; a timeout (missed everything) fades out with no field.
    public class IllusionVenomOrb : ModProjectile
    {
        public const float Gravity = 0.2f;
        public const float MaxFallSpeed = 12f;
        public const int LifetimeTicks = 240;
        private const int FadeTicks = 15;

        public static readonly Color VenomColor = new Color(60, 190, 40);

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        // Harmless while fading out after a whiff.
        public override bool? CanDamage()
        {
            if (Projectile.timeLeft <= FadeTicks)
            {
                return false;
            }

            return null;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);
            }
        }

        public override void AI()
        {
            // Velocity is updated before the move, so the drop after N ticks is Gravity * N(N+1)/2 (the caster's solver uses this).
            Projectile.velocity.Y = MathHelper.Min(Projectile.velocity.Y + Gravity, MaxFallSpeed);
            Projectile.rotation += 0.25f * (Projectile.velocity.X >= 0f ? 1f : -1f);

            if (Projectile.timeLeft <= FadeTicks)
            {
                Projectile.alpha = (int)(255f * (1f - Projectile.timeLeft / (float)FadeTicks));
            }

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, VenomColor.ToVector3() * 0.35f);

            if (Projectile.timeLeft % 2 == 0)
            {
                Dust trail = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
                    -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(0.4f, 0.4f), 80, VenomColor, 0.9f);
                trail.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color drawColor = Color.White * (1f - Projectile.alpha / 255f);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            // timeLeft 0 = the lifetime ran out in mid-air: no field, the fade above already played.
            bool splashed = timeLeft > 0;

            if (!Main.dedServ)
            {
                int burstCount = 40;

                if (!splashed)
                {
                    burstCount = 12;
                }

                for (int i = 0; i < burstCount; i++)
                {
                    Dust burst = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
                        Main.rand.NextVector2Circular(4f, 4f), 60, VenomColor, Main.rand.NextFloat(0.8f, 1.4f));
                    burst.noGravity = true;
                }

                if (splashed)
                {
                    SoundEngine.PlaySound(SoundID.Item107 with { Volume = 0.6f, Pitch = -0.3f }, Projectile.Center);
                }
            }

            // Server-owned like the orb itself. If a client's local player was hit, that client killed only its copy; the
            // server's copy keeps falling and splashes on the floor at the player's feet.
            if (!splashed || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 fieldCenter = IllusionVenomField.FindFloorCenter(Projectile.Center);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), fieldCenter, Vector2.Zero,
                ModContent.ProjectileType<IllusionVenomField>(), Projectile.damage, 0f, Main.myPlayer);
        }
    }
}
