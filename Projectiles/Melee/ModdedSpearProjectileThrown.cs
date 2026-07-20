using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee
{
    // Parent class for thrown spears where hitbox is centered on spear tip
    public abstract class ModdedSpearProjectileThrown : ModProjectile
    {
        // Size of the hitbox at the tip of the spear
        public abstract int HitboxSize { get; }
        // Armor shader applied when `Projectile.ai[0] == 1`
        public abstract int ChargedShaderID { get; }

        // See Projectile.extraUpdates
        public virtual int ExtraUpdates { get => 0; }
        // See Projectile.light
        public virtual float Light { get => 0f; }
        // See Projectile.penetrate
        public virtual int Penetrate { get => 1; }
        // See Projectile.scale
        public virtual float Scale { get => 1f; }
        // See Projectile.timeLeft
        public virtual int TimeLeft { get => 3600; }

        // Dust drawing logic to use in AI(), if any
        public virtual void AIDust() { }
        // Dust drawing logic to use in PreKill(), if any
        public virtual void PreKillDust() { }

        public override void SetDefaults()
        {
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.height = HitboxSize;
            Projectile.light = Light;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.scale = Scale;
            Projectile.width = HitboxSize;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.extraUpdates = ExtraUpdates;
            Projectile.penetrate = Penetrate;
        }

        // Texture was being static cached in child classes but leaked when the logic was moved to
        // a parent class. Making it an instance variable for now, but if this ends up being an
        // efficiency issue then can probably set a static texture map / dictionary here, or have
        // children provide their static texture.
        public Texture2D texture;
        public override void OnSpawn(IEntitySource source)
        {
            // Since projectile is centered on spear tip for collision hitbox, offset starting
            // projectile center so sprite matches where projectile is initially held.
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture);
            }
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            float initialRotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.ToRadians(45f);
            Vector2 spearTipOffset = origin.RotatedBy(initialRotation);
            Projectile.Center -= spearTipOffset;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (Projectile.ai[0] == 1)
            {
                ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ChargedShaderID), Main.LocalPlayer);
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
            Vector2 spearTipOffset = origin.RotatedBy(Projectile.rotation);
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY) + spearTipOffset,
                sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.ToRadians(45f); //This makes it rotate to face where it's moving
            //projectile.velocity.Y += (9.8f / 60); //This is its gravity. Comes out to about 0.16 per frame, which is actually really high!!
            Projectile.velocity.Y += 0.1f;

            AIDust();
        }

        public override bool PreKill(int timeleft)
        {
            Projectile.type = ProjectileID.WoodenArrowHostile;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            PreKillDust();
            return true;
        }
    }
}
