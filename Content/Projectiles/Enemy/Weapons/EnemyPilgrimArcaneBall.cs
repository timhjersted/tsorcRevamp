using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.Triad;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class EnemyPilgrimArcaneBall : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(HomingStarStar));

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

        // Cursed-fire palette (Cursed Dragon's through-line): red flame with the plum powerful-curse accent.
        // CursedFireRed is the shader's beam tint; the shader brightens it toward white at the core.
        private static readonly Vector3 CursedFireRed = new Vector3(0.45f, 0.05f, 0.12f);
        // Shrinks out over its last 12 ticks instead of vanishing at full size on timeout.
        private const int FadeOutTicks = 12;

        public override void AI()
        {
            int dust1 = Dust.NewDust(Projectile.Center - new Vector2(2, 13), 1, 1, DustID.RedTorch, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.2f);
            Main.dust[dust1].noGravity = true;
            Main.dust[dust1].velocity *= 0.2f;
            int dust2 = Dust.NewDust(Projectile.Center + new Vector2(2, 10), 1, 1, DustID.RedTorch, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.2f);
            Main.dust[dust2].noGravity = true;
            Main.dust[dust2].velocity *= 0.2f;

            if (Main.rand.NextBool(4))
            {
                PowerfulCurseBuildup.SpawnAccentDust(Projectile.Center, 0, 0, Projectile.velocity * 0.15f, 1.1f);
            }

            if (Projectile.timeLeft < FadeOutTicks)
            {
                Projectile.scale = Projectile.timeLeft / (float)FadeOutTicks;
            }

            Lighting.AddLight(Projectile.Center, 0.7f, 0.12f, 0.15f);
        }

        // Regular (not powerful) curse: the ball is the dragon's common shot, the Poison Cloud is the big curse source.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 36000, false);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 15; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RedTorch, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 100, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }

            for (int i = 0; i < 6; i++)
            {
                PowerfulCurseBuildup.SpawnAccentDust(Projectile.Center, 0, 0, Main.rand.NextVector2Circular(2.5f, 2.5f), 1.3f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            data ??= new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/PilgrimArcaneBall", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "CursedFireballPass").UseColor(CursedFireRed);

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
