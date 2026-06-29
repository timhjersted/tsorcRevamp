using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    public class EnemyPilgrimArcaneBall : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        private ArmorShaderData data;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.timeLeft = 90;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
        }

        public override void AI()
        {
            int dust1 = Dust.NewDust(Projectile.Center - new Vector2(2, 13), 1, 1, 88, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.2f);
            Main.dust[dust1].noGravity = true;
            Main.dust[dust1].velocity *= 0.2f;
            int dust2 = Dust.NewDust(Projectile.Center + new Vector2(2, 10), 1, 1, 88, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.2f);
            Main.dust[dust2].noGravity = true;
            Main.dust[dust2].velocity *= 0.2f;
            Lighting.AddLight(Projectile.Center, 0.2f, 0.2f, 0.7f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 120);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 15; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 88, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 100, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            data ??= new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/PilgrimArcaneBall", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "CursedFireballPass");

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)Projectile.width * 4, (int)Projectile.height * 4);
            Vector2 origin = sourceRectangle.Size() / 2f;
            data.UseTargetPosition(sourceRectangle.Size());
            data.Apply(null);

            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.velocity.ToRotation() + MathHelper.Pi, origin, Projectile.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
