using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class UlhanExplosiveArrow : ModProjectile
    {
        public const float MaximumTravel = 15f * 16f;
        private const float EmbeddedState = 2f;
        // PLACEHOLDER sprite: copy of Content/Items/Ammo/CruelArrow.png; 14x44, top leads.
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 120;
            Projectile.aiStyle = -1;
        }

        // The blast owns the entire 75-damage hit. Player proximity is detected on authority,
        // since hostile OnHitPlayer runs on the victim's client and cannot spawn a server blast.
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Projectile.ai[1] == 1f)
            {
                Projectile.Kill();
                return;
            }
            bool embedded = Projectile.ai[1] == EmbeddedState;
            if (embedded)
            {
                // A lodged arrow stays armed until a player approaches, rather than expiring
                // into an unprompted blast. ai[1] and netUpdate replicate the stuck state.
                Projectile.velocity = Vector2.Zero;
                Projectile.timeLeft = 2;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (Player player in Main.ActivePlayers)
                {
                    if (!player.dead && !player.ghost
                        && UlhanFireExplosion.IsWithinDamageRadius(Projectile.Center, player.Hitbox))
                    {
                        Projectile.Kill();
                        return;
                    }
                }
            }
            if (embedded)
            {
                EmitEmbers();
                return;
            }
            if (++Projectile.ai[2] > 15f)
                Projectile.velocity.Y += 0.1f;
            float remaining = MaximumTravel - Projectile.ai[0];
            float step = Projectile.velocity.Length();
            if (step >= remaining)
            {
                Projectile.velocity *= remaining / step;
                step = remaining;
                Projectile.ai[1] = 1f;
            }
            Projectile.ai[0] += step;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            EmitEmbers();
        }

        private void EmitEmbers()
        {
            if (Main.dedServ)
                return;
            Lighting.AddLight(Projectile.Center, 0.55f, 0.22f, 0.03f);
            if (Projectile.ai[1] == EmbeddedState && Main.GameUpdateCount % 6 != 0)
                return;
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.OrangeTorch,
                -Projectile.velocity * 0.1f, 80, default, 0.7f);
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // The copied Cruel Arrow art has its lateral detail on the wrong side for this shot.
            var texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
                lightColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale,
                Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally);
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Deliberately lodge in terrain; tile impact never detonates this payload.
            Projectile.velocity = Vector2.Zero;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.ai[1] = EmbeddedState;
                Projectile.timeLeft = 2;
                Projectile.netUpdate = true;
            }
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<UlhanFireExplosion>(), Projectile.damage, Projectile.knockBack, Main.myPlayer);
        }
    }
}
