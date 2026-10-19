using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Projectiles.Enemy.Archdeacon
{
    // Archdeacon's Glintstone Eruption lance, spawned buried by ArchdeaconGlintRune and bursting up out of the floor.
    // A 32x264 needle of light (3x the original 88px, requested 2026-10-18); the hitbox matches the visual.
    // Look: the ArchdeaconGlintLance shader by default. /glintlegacy draws ArchdeaconGlintLance.png instead (a copy of
    // IlluminantHomingStar, 34x90, round head at the BOTTOM) flipped head-up and stretched 3x to the hitbox.
    // Drawn behind tiles to hide the buried part.
    // Life: rises at RiseSpeed with RiseDrag per tick (total rise ~ RiseSpeed / (1 - RiseDrag) = 250px of its 264px):
    // 100px in its first 6t, so it bursts out of the floor; then it stands.
    // Death: fades over FadeTicks and bursts into sparkles. Deliberately ignores tiles: it is born inside the floor.
    public class ArchdeaconGlintLance : ModProjectile
    {
        public const float RiseSpeed = 20f;
        public const float HalfHeight = 132f;
        private const float RiseDrag = 0.92f;
        // 30t solid (damaging) + a 30t fade. The fade was 15t; lifetime grew with it so the solid window is unchanged.
        private const int LifetimeTicks = 60;
        private const int FadeTicks = 30;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = (int)(HalfHeight * 2f);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hide = true;
        }

        // Harmless while fading so the dissolving lance can't hit someone walking into it.
        public override bool? CanDamage()
        {
            if (Projectile.timeLeft <= FadeTicks)
            {
                return false;
            }

            return null;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.6f, Pitch = -0.2f }, Projectile.Center);

                Vector2 surface = Projectile.Center - new Vector2(0f, HalfHeight);
                for (int i = 0; i < 14; i++)
                {
                    Vector2 burstVelocity = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-5f, -2f));
                    Dust burst = Dust.NewDustPerfect(surface + new Vector2(Main.rand.NextFloat(-16f, 16f), 0f), DustID.TintableDustLighted,
                        burstVelocity, 20, ArchdeaconGlintStar.GlintCore, 1.2f);
                    burst.noGravity = true;
                }
            }
        }

        public override void AI()
        {
            Projectile.velocity *= RiseDrag;

            if (Projectile.timeLeft <= FadeTicks)
            {
                Projectile.alpha = (int)(255 * (1f - Projectile.timeLeft / (float)FadeTicks));
            }

            Lighting.AddLight(Projectile.Center, ArchdeaconGlintStar.GlintColor.ToVector3() * 0.6f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            ArchdeaconGlintStar.ApplyHitDebuffs(target);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles,
            List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Default look: the ArchdeaconGlintLance shader on the hitbox (tip up); /glintlegacy = the comet sprite.
            if (!GlintLegacyCommand.UseLegacySprites)
            {
                EnemyVFX.DrawArchdeaconGlintLance(Projectile.Center, Projectile.Size, 1f - Projectile.alpha / 255f);
                return false;
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float opacity = 1f - Projectile.alpha / 255f;
            Color glow = new Color(255, 255, 255, 0) * opacity;

            Vector2 stretchToHitbox = new Vector2(1f, Projectile.height / (float)texture.Height);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, glow, MathHelper.Pi,
                texture.Size() * 0.5f, stretchToHitbox, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 20; i++)
            {
                Vector2 point = Projectile.position + new Vector2(Main.rand.NextFloat(Projectile.width), Main.rand.NextFloat(Projectile.height * 0.6f));
                Dust sparkle = Dust.NewDustPerfect(point, DustID.TintableDustLighted, Main.rand.NextVector2Circular(2f, 2f), 40,
                    ArchdeaconGlintStar.GlintColor, 1f);
                sparkle.noGravity = true;
            }
        }
    }
}
