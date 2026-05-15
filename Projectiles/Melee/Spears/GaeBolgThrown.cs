using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class GaeBolgThrown : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.height = 24;
            Projectile.light = 0.6f;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.scale = 1.1f;
            Projectile.penetrate = 4;
            Projectile.width = 24;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 240;
        }

        public static Texture2D texture;
        public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (Projectile.ai[0] == 1)
            {
                ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.StardustDye), Main.LocalPlayer);
                data.Apply(null);
            }

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture);
            }

            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.ToRadians(45f); //This makes it rotate to face where it's moving
            //projectile.velocity.Y += (9.8f / 60); //This is its gravity. Comes out to about 0.16 per frame, which is actually really high!!
            Projectile.velocity.Y += 0.1f;

            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, Projectile.velocity.X * -0.2f, Projectile.velocity.Y * -0.2f, 70, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.ai[0] == 1)
            {
                target.AddBuff(BuffID.Frostburn2, 210);
            }
        }

        public override void PostDraw(Player player, Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f); 
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 1; i <= 4; i++)  
            {
                Vector2 offset = Projectile.velocity * -i * 1f;  
                float alpha = 0.35f * (1f - (i / 6f));  

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    null,
                    lightColor * alpha,  
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0
                );
            }
        }

        public override bool PreKill(int timeleft)
        {
            Projectile.type = ProjectileID.WoodenArrowHostile;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, 0, 0, 0, default, 1f);
            }
            return true;
        }
    }
}