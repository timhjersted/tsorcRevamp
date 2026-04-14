using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Magic
{
    class AbyssalDelugeBall : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Cursed Fireball");
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.scale = 1.35f;
        }
        bool playedSound = false;

        public override void AI()
        {
            int dust = Dust.NewDust(
            Projectile.position,
            Projectile.width,
            Projectile.height,
            229,
            Projectile.velocity.X,
            Projectile.velocity.Y,
            90,
            new Color(0, 116, 128),
            2.8f
            );
            Main.dust[dust].noGravity = true;
            Projectile.rotation += Projectile.direction * 0.8f;
            Lighting.AddLight(Projectile.Center, 0.2f, 0.6f, 0.7f);

            if (Projectile.velocity.X <= 22f && Projectile.velocity.Y <= 22f && Projectile.velocity.X >= -22f && Projectile.velocity.Y >= -22f)
            {
                Projectile.velocity.X *= 1.06f;
                Projectile.velocity.Y *= 1.06f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AbyssalSinking>(), 7 * 60);
        }
        
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 15; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 229, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 100, new Color(0, 116, 128), 2.8f);
                Main.dust[dust].noGravity = true;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, 0, 0, ModContent.ProjectileType<AbyssalDeluge>(), Projectile.damage / 2, 6f, Projectile.owner);
            }
        }

        ArmorShaderData data;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            if (data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/AbyssalDelugeBall", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "CursedFireballPass");
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)Projectile.width * 4, (int)Projectile.height * 4);
            Vector2 origin = sourceRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            data.UseTargetPosition(sourceRectangle.Size());

            //Apply the shader
            data.Apply(null);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.velocity.ToRotation() + MathHelper.Pi, origin, Projectile.scale, spriteEffects, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
