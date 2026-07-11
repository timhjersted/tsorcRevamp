using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    class SwordOfLordGwynSlash : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Broadswords/SwordOfGwyn";

        const float BladeLength = 142f;
        const float BladeWidth = 42f;

        bool DashSlash => Projectile.ai[0] == 1f;
        int Direction => Projectile.ai[1] >= 0f ? 1 : -1;
        int Lifetime => DashSlash ? 28 : 22;
        float Timer => Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.NoMeleeSpeedVelocityScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.penetrate = -1;
            Projectile.hide = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 28;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.localAI[0]++;
            Projectile.timeLeft = System.Math.Min(Projectile.timeLeft, Lifetime - (int)Timer);
            Projectile.Center = player.RotatedRelativePoint(player.MountedCenter, false, false);
            Projectile.rotation = CurrentAngle();
            Projectile.spriteDirection = Direction;
            player.heldProj = Projectile.whoAmI;

            player.itemTime = 2;
            player.itemAnimation = 2;
            player.ChangeDir(Direction);

            if (DashSlash)
            {
                player.ResetMeleeHitCooldowns();
            }

            Vector2 start = BladeStart();
            Vector2 end = BladeEnd();
            for (int i = 0; i < (DashSlash ? 5 : 3); i++)
            {
                Vector2 pos = Vector2.Lerp(start, end, Main.rand.NextFloat());
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(pos - new Vector2(3f), 6, 6, type, player.velocity.X * 0.1f, player.velocity.Y * 0.1f, 60, default, DashSlash ? 1.9f : 1.35f);
                Main.dust[dust].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.8f, 0.42f, 0.12f);

            if (Timer >= Lifetime)
            {
                Projectile.Kill();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), BladeStart(), BladeEnd(), BladeWidth, ref point);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (DashSlash)
            {
                modifiers.Knockback *= 1.6f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, DashSlash ? 7 * 60 : 5 * 60);
            if (DashSlash)
            {
                target.AddBuff(BuffID.Daybreak, 3 * 60);
            }
        }

        float CurrentAngle()
        {
            float progress = MathHelper.Clamp(Timer / Lifetime, 0f, 1f);
            progress = progress * progress * (3f - 2f * progress);
            float baseAngle = DashSlash
                ? MathHelper.Lerp(0.95f, -1.35f, progress)
                : MathHelper.Lerp(-2.25f, 0.75f, progress);
            return Direction == 1 ? baseAngle : MathHelper.Pi - baseAngle;
        }

        Vector2 BladeStart()
        {
            return Projectile.Center + new Vector2(Direction * 6f, -2f);
        }

        Vector2 BladeEnd()
        {
            return BladeStart() + CurrentAngle().ToRotationVector2() * BladeLength;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = new Vector2(texture.Width * 0.45f, texture.Height * 0.85f);
            SpriteEffects effects = Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float drawRotation = Projectile.rotation + (Direction == 1 ? MathHelper.PiOver4 : -MathHelper.PiOver4);
            Color color = Color.White * MathHelper.Clamp(Projectile.timeLeft / 6f, 0.35f, 1f);
            Main.EntitySpriteDraw(texture, BladeStart() - Main.screenPosition, null, color, drawRotation, origin, DashSlash ? 1.18f : 1f, effects, 0);
            return false;
        }
    }
}
