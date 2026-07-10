using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Stage 1 of Gwyn's Spear of the First Sun: the thrown lightning spear (LightningSpear.png, 4
    ///frames). Flies fast toward the locked throw vector, exploding on contact with ANYTHING — tile
    ///or player — into a lightning burst, and plants a GwynLightningStrike at the impact point (which
    ///delays, then calls a bolt down + runs electricity along the floor). Applies On Fire! + a jolt.
    ///</summary>
    class GwynLightningSpear : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/GwynLightningSpear";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 40;   // tight hitbox vs the big sprite
            Projectile.height = 24;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 240;
            Projectile.light = 0.8f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            //Crackle animation
            if (++Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }
            //Gold lightning trail
            for (int i = 0; i < 2; i++)
            {
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f, 60, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.9f, 0.8f, 0.35f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Explode();
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 5 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            Explode();
        }

        bool _exploded;
        void Explode()
        {
            if (_exploded)
            {
                return;
            }
            _exploded = true;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.7f, Pitch = 0.2f }, Projectile.Center); //electric zap
            for (int i = 0; i < 24; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, vel.X, vel.Y, 40, default, 1.6f);
                Main.dust[dust].noGravity = true;
            }
            //Plant the delayed ground-strike at the impact point (it finds the floor + telegraphs)
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<GwynLightningStrike>(), Projectile.damage, 0f, Projectile.owner);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);
            //Sprite art runs horizontally; rotation aligns it to travel
            SpriteEffects fx = Projectile.velocity.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, 0.7f, fx, 0);
            return false;
        }
    }
}
