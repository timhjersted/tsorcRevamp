using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.Items.Weapons.Melee.Spears;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class PilgrimArcaneBall : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Cursed Fireball");
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.timeLeft = 60;
            Projectile.ignoreWater = true;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.penetrate = 1;
        }
        bool playedSound = false;

        public override void AI()
        {
            // empowered projectile 
            if (Main.player[Projectile.owner].HasBuff(ModContent.BuffType<PilgrimSpontoonBuff>()))
            {
                Projectile.penetrate = 3; 

                int dust1 = Dust.NewDust(Projectile.Center - new Vector2(2, 13), 1, 1, 88, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.2f);
                Main.dust[dust1].noGravity = true;
                Main.dust[dust1].velocity *= 0.2f;
                int dust2 = Dust.NewDust(Projectile.Center + new Vector2(2, 10), 1, 1, 88, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1.2f);
                Main.dust[dust2].noGravity = true;
                Main.dust[dust2].velocity *= 0.2f;
                Projectile.localAI[0] = 0;
                Lighting.AddLight(Projectile.Center, 0.2f, 0.2f, 0.7f);

                if (!playedSound)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.5f }, Projectile.Center);
                        playedSound = true;
                    }
            }
            else 
            {
                Projectile.penetrate = 1;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.player[Projectile.owner].HasBuff(ModContent.BuffType<PilgrimSpontoonBuff>()))
            {
                target.AddBuff(BuffID.Frostburn, 120);
            }
        }
        
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 15; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 88, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 100, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }
        }

        ArmorShaderData data;
        public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            if (data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/PilgrimArcaneBall", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "CursedFireballPass");
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
