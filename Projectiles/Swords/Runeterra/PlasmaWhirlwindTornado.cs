using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ReLogic.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace tsorcRevamp.Projectiles.Swords.Runeterra
{
    public class PlasmaWhirlwindTornado : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 180;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
        }

        SlotId SoundSlotID;
        SoundStyle TornadoSoundStyle = SoundID.DD2_BookStaffTwisterLoop;
        bool soundPaused;
        bool playedSound = false;
        ActiveSound TornadoSound;
        private static int numrings = 30;
        private static Vector2 ringScale = new Vector2(0.2f);
        private int frameTimer;
        private int currentFrame;
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            Projectile.damage = (int)(player.GetWeaponDamage(player.HeldItem) * 2f);
            Projectile.localNPCHitCooldown = (int)(5.5f / (0.5f + (player.GetTotalAttackSpeed(DamageClass.Melee) / 2f)));
        }
        public override void AI()
        {
            if (!playedSound)
            {
                playedSound = true;
                SoundSlotID = SoundEngine.PlaySound(SoundID.DD2_BookStaffTwisterLoop, Projectile.Center); //can give funny pitch hehe
                if (TornadoSound == null)
                {
                    SoundEngine.TryGetActiveSound(SoundSlotID, out TornadoSound);
                }
                else
                {
                    if (SoundEngine.AreSoundsPaused && !soundPaused)
                    {
                        TornadoSound.Pause();
                        soundPaused = true;
                    }
                    else if (!SoundEngine.AreSoundsPaused && soundPaused)
                    {
                        TornadoSound.Resume();
                        soundPaused = false;
                    }
                    TornadoSound.Position = Projectile.Center;
                }
            }
            int dustID = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.CoralTorch, Scale: 2);
            Main.dust[dustID].noGravity = true;
        }
        public override void Kill(int timeLeft)
        {
            if (Projectile.timeLeft < 2)
            {
                if (TornadoSound != null && TornadoSound.IsPlaying)
                {
                    TornadoSound.Stop();
                }
            }
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            Projectile.damage = (int)(Projectile.damage * 0.95f); //Multihit penalty
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (frameTimer == 0)
            {
                frameTimer = 3;
                currentFrame++;
            }
            if (frameTimer > 0)
            {
                frameTimer--;
            }
            Texture2D tex = (Texture2D)TextureAssets.Projectile[Projectile.type];
            //End old spritebatch
            Main.spriteBatch.End();
            //Start new one in texture sort mode. You'll also need to restart it again to change from alpha to additive blending
            Main.spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Draw code goes here
            for (var j = 0; j < 1; j++) //this would be changed if you needed the tornado to redraw itself multiple times. looked neat with random offset but also makes the tornado brighter white
            {
                for (var i = 0; i < numrings*2; i++) //some weird stuff going on with ring offset i think. multiplying numrings by 2 to make it taller for now
                {
                    float _xscale = ((ringScale.X * i/2) * 0.1f) + 0.005f * i; //controls how wide each individual ring will be
                    float _yscale = 0.5f; //controls how tall each individual ring will be
                    var _offset = (Math.Sin(Projectile.timeLeft / 6 + i / 4) * 24) * _xscale; //this is how the rings gain a wave effect
                    var _dist = 2f + (float)i * 2.5f; //distance between each ring
                    float _rot = (-numrings) + i; //the rotation of a given ring to make it look just a little more dynamic
                    _rot *= -((float)Math.PI / 180); //convert to radians cus terraria is like that
                    var vec = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f)).RotatedBy(_rot); //ring position relative to projectile position. random offset to make it look chaotic
                    vec += new Vector2(0, 70);
                    vec.X += (float)_offset; //apply wave
                    vec.Y -= _dist; //apply ring separation
                    var frameIndex = (currentFrame + i / 2) % Main.projFrames[Projectile.type]; //get frame index to draw
                    var sourceRect = new Rectangle(frameIndex * 128, 0, 127, 128); //the frame itself
                    var col = new Color(0.498f, 1f, 0.831f, 0.3f); //color
                    //draw the ring
                    Main.spriteBatch.Draw(tex, Projectile.Center + vec - Main.screenPosition, sourceRect, col, _rot * Math.Sign(Projectile.velocity.X), new Vector2(64, 64), ringScale + new Vector2(_xscale, _yscale), SpriteEffects.None, 0);
                }
            }


            //Restart it again with the normal parameters to avoid interfering with the rest of the games normal draw loop
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}