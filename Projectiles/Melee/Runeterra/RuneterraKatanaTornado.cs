using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;

namespace tsorcRevamp.Projectiles.Melee.Runeterra
{
    public class RuneterraKatanaTornado : ModProjectile
    {
        public bool AppliedOnSpawn = false;
        public bool Hit = false;
        public const int baseTimeLeft = 180;
        public string SoundPath;
        public int dustID;
        public float yScale;
        public Color Color;
        public int numrings;
        public float _xscalebonus = 0;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
            ProjectileID.Sets.NoMeleeSpeedVelocityScaling[Projectile.type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 160;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = baseTimeLeft;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = (Projectile.velocity == Vector2.Zero) ? 6 : 5;
        }
        private static Vector2 ringScale = new Vector2(0.2f);
        private int frameTimer;
        private int currentFrame;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 2;
        }
        public override void OnSpawn(IEntitySource source)
        {
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            
            if (!AppliedOnSpawn)
            {
                float baseScale = 1f;
                Projectile.scale = player.GetAdjustedItemScale(player.HeldItem);
                switch (Projectile.ai[0])
                {
                    case 1:
                    {
                        dustID = DustID.Smoke;
                        baseScale = SteelTempest.BaseScale;
                        
                        yScale = 0.57f * Projectile.scale;
                        numrings = (int)(40 * Projectile.scale);
                        Color = new Color(1, 1, 1, 0.3f);
                        break;
                    }
                    case 2:
                    {
                        dustID = DustID.CoralTorch;
                        baseScale = PlasmaWhirlwind.BaseScale;
                        Projectile.width = 120;
                        Projectile.height = 166;
                        
                        yScale = 0.32f * Projectile.scale;
                        numrings = (int)(32 * Projectile.scale);
                        _xscalebonus = 0.1f * Projectile.scale;
                        Color = new Color(0.498f, 1f, 0.831f, 0.3f);
                        break;
                    }
                    case 3:
                    {
                        dustID = DustID.Torch;
                        baseScale = Nightbringer.BaseScale;
                        Projectile.width = 140;
                        Projectile.height = 190;
                        
                        yScale = 0.9f * Projectile.scale;
                        numrings = (int)(40 * Projectile.scale);
                        _xscalebonus = 0.19f * Projectile.scale;
                        Color = new Color(0.886f, 0.245f, 0.033f, 0.3f);
                        break;
                    }
                }
                Projectile.Resize((int)(Projectile.width / baseScale * Projectile.scale), (int)(Projectile.height / baseScale * Projectile.scale));
                AppliedOnSpawn = true;
            }
            Vector2 mountedCenter = player.MountedCenter + new Vector2(-player.width / 2, 0);
            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustID, Scale: 2);
            dust.noGravity = true;
            if (Projectile.timeLeft == baseTimeLeft - 40)
            {
                player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks = 0;
            }
            if (Projectile.velocity == Vector2.Zero && Projectile.timeLeft > baseTimeLeft - PlasmaWhirlwind.DashDuration * 60)
            {
                Projectile.Center = mountedCenter;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            float SoundVolume = 0;
            if (!Hit)
            {
                Hit = true;
                switch (Projectile.ai[0])
                {
                    case 1:
                        {
                            SoundPath = "tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/";
                            SoundVolume = 0.5f;
                            break;
                        }
                    case 2:
                        {
                            SoundPath = "tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/";
                            SoundVolume = 0.7f;
                            break;
                        }
                    case 3:
                        {
                            SoundPath = "tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/";
                            SoundVolume = 0.7f;
                            break;
                        }
                }
                SoundEngine.PlaySound(new SoundStyle(SoundPath + "TornadoHit") with { Volume = SoundVolume });
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity != Vector2.Zero)
            {
                _xscalebonus = 0;
            }
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
                    Main.spriteBatch.Draw(tex, Projectile.Center + vec - Main.screenPosition, sourceRect, Color, _rot * Math.Sign(Projectile.velocity.X), new Vector2(64, 64), ringScale + new Vector2(_xscale + _xscalebonus, yScale), SpriteEffects.None, 0);
                }
            }


            //Restart it again with the normal parameters to avoid interfering with the rest of the games normal draw loop
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}