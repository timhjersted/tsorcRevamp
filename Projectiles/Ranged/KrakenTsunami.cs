using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Steamworks;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Ranged
{
    public class KrakenTsunami : ModProjectile
    {
        SoundStyle WaterSoundStyle;
        SlotId WaterSoundID;
        public bool SoundTimerIsRunning = false;
        public int SoundTimer = 0;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 240;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Projectile.SentryLifeTime;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.ignoreWater = true;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
        }
        public override void OnSpawn(IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.Item21 with { Volume = 1.5f });
            WaterSoundStyle = SoundID.Waterfall with { Volume = 1f, IsLooped = false, MaxInstances = 1 };
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];

                if (i != Projectile.whoAmI && other.type == ModContent.ProjectileType<KrakenTsunami>() && other.owner == Projectile.owner)
                {
                    other.Kill();
                    break;
                }
            }
        }
        public override void AI()
        {
            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Water, Scale: 2);
            dust.noGravity = true;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];

                if (i != Projectile.whoAmI && other.active && other.friendly && Projectile.Hitbox.Intersects(other.Hitbox) && other.DamageType == DamageClass.Ranged && !other.GetGlobalProjectile<tsorcGlobalProjectile>().KrakenEmpowered && other.type != ModContent.ProjectileType<KrakenTsunamiShark>())
                {
                    other.GetGlobalProjectile<tsorcGlobalProjectile>().KrakenEmpowered = true;
                    Projectile Shark = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<KrakenTsunamiShark>(), Projectile.originalDamage, Projectile.knockBack, Projectile.owner);
                    Shark.CritChance = Projectile.CritChance;
                    if (!SoundEngine.TryGetActiveSound(WaterSoundID, out var ActiveSound))
                    {
                        WaterSoundID = SoundEngine.PlaySound(WaterSoundStyle with { Volume = 0.5f });
                        SoundTimerIsRunning = true;
                    }
                    break;
                }
            }
            if (SoundTimerIsRunning)
            {
                SoundTimer++;
            }
            if (SoundTimer > 10 && SoundEngine.TryGetActiveSound(WaterSoundID, out var activeSound))
            {
                activeSound.Stop();
                SoundTimerIsRunning = false;
                SoundTimer = 0;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Waterfall with { Volume = 0.5f });
        }
        private int frameTimer;
        private int currentFrame;
        private static Vector2 ringScale = new Vector2(0.2f);
        public float yScale = 0.9f;
        public int numrings = 40;
        public Color Color = Color.MidnightBlue;
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
            Texture2D tex = (Texture2D)TextureAssets.Projectile[Type];
            //End old spritebatch
            Main.spriteBatch.End();
            //Start new one in texture sort mode. You'll also need to restart it again to change from alpha to additive blending
            Main.spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Draw code goes here
            for (var j = 0; j < 1; j++) //this would be changed if you needed the tornado to redraw itself multiple times. looked neat with random offset but also makes the tornado brighter white
            {
                for (var i = 0; i < numrings * 2; i++) //some weird stuff going on with ring offset i think. multiplying numrings by 2 to make it taller for now
                {
                    float _xscale = ringScale.X * i / 2 * 0.1f + 0.01f * i; //controls how wide each individual ring will be
                    var _offset = Math.Sin(Projectile.timeLeft / 6 + i / 4) * 24 * _xscale; //this is how the rings gain a wave effect
                    var _dist = 2f + i * 2.5f; //distance between each ring
                    float _rot = -numrings + i; //the rotation of a given ring to make it look just a little more dynamic
                    _rot *= -((float)Math.PI / 180); //convert to radians cus terraria is like that
                    var vec = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f)).RotatedBy(_rot); //ring position relative to projectile position. random offset to make it look chaotic
                    vec += new Vector2(0, 60);
                    vec.X += (float)_offset; //apply wave
                    vec.Y -= _dist; //apply ring separation
                    var frameIndex = (currentFrame + i / 2) % Main.projFrames[Projectile.type]; //get frame index to draw
                    var sourceRect = new Rectangle(frameIndex * 128, 0, 127, 128); //the frame itself
                    //draw the ring
                    Main.spriteBatch.Draw(tex, Projectile.Center + vec - Main.screenPosition, sourceRect, Color, _rot * Math.Sign(Projectile.velocity.X), new Vector2(64, 64), ringScale + new Vector2(_xscale, yScale), SpriteEffects.None, 0);
                }
            }


            //Restart it again with the normal parameters to avoid interfering with the rest of the games normal draw loop
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}