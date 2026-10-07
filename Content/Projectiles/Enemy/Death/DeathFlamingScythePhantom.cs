using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Death
{
    /// <summary>
    /// Non-damaging visual preview of DeathFlamingScythe. It keeps the same scale and
    /// hitbox metadata, spins at the same speed, then fades out.
    /// </summary>
    class DeathFlamingScythePhantom : ModProjectile
    {
        const int PhantomAlpha = 178; // 30% opacity
        const int FadeOutTicks = 15;
        const float BoomerangThrowDistance = 800f;
        const int BoomerangOutboundTicks = 60;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FlamingScythe;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void SetDefaults()
        {
            Projectile.scale = 3f;
            Projectile.ai[0] = (float)DeathFlamingScythe.MovementMode.Boomerang;
            Texture2D texture = ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Projectile.width = (int)(texture.Width * 0.8f);
            Projectile.height = (int)(texture.Height * 0.8f);
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            int movementMode = (int)Projectile.ai[0];
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (movementMode == (int)DeathFlamingScythe.MovementMode.Straight)
            {
                Projectile.Center += Projectile.velocity;
            }
            else
            {
                Vector2 origin = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                float progress = MathHelper.Clamp(Projectile.localAI[0] / (float)BoomerangOutboundTicks, 0f, 1f);
                float eased = 1f - (1f - progress) * (1f - progress);
                Projectile.Center = origin + direction * BoomerangThrowDistance * eased;
            }
            Projectile.rotation += MathHelper.ToRadians(12f);
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeOutTicks, 0f, 1f);
            Projectile.alpha = (int)MathHelper.Lerp(255f, PhantomAlpha, fade);
            float opacity = 1f - Projectile.alpha / 255f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.22f, 0.08f) * opacity);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[ProjectileID.FlamingScythe].Value;
            float opacity = 1f - Projectile.alpha / 255f;
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor * opacity,
                Projectile.rotation,
                texture.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None,
                0f);
            return false;
        }

        public static int Spawn(
            IEntitySource source,
            Vector2 position,
            Vector2 direction,
            int lifetimeTicks,
            DeathFlamingScythe.MovementMode movementMode = DeathFlamingScythe.MovementMode.Boomerang,
            float speed = 0f)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return -1;
            }

            int movementModeValue = (int)movementMode;
            Vector2 normalizedDirection = direction.SafeNormalize(Vector2.UnitX);
            Vector2 velocity = movementMode == DeathFlamingScythe.MovementMode.Straight
                ? normalizedDirection * speed
                : normalizedDirection;
            int index = Projectile.NewProjectile(
                source,
                position,
                velocity,
                ModContent.ProjectileType<DeathFlamingScythePhantom>(),
                0,
                0f,
                Main.myPlayer,
                movementModeValue,
                position.X,
                position.Y);

            if (index >= 0 && index < Main.maxProjectiles)
            {
                Main.projectile[index].timeLeft = lifetimeTicks;
                Main.projectile[index].netUpdate = true;
            }

            return index;
        }
    }
}
