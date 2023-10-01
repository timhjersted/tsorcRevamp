using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;

namespace tsorcRevamp.Projectiles.Melee.Runeterra
{
    public class NightbringerTornado : ModProjectile
    {
        public bool Hit = false;
        public const int baseTimeLeft = 180;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
            ProjectileID.Sets.NoMeleeSpeedVelocityScaling[Projectile.type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 180;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = baseTimeLeft;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
        }
        private static int numrings = 30;
        private static Vector2 ringScale = new Vector2(0.2f);
        private int frameTimer;
        private int currentFrame;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 2;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Vector2 mountedCenter = player.MountedCenter + new Vector2(-player.width / 2, 0);
            int dustID = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, Scale:2);
            Main.dust[dustID].noGravity = true;
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
            target.AddBuff(BuffID.OnFire3, 600);
            Projectile.damage = (int)(Projectile.damage * 0.95f); //Multihit penalty
            if (!Hit)
            {
                Hit = true;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/TornadoHit") with { Volume = 1f });
            }
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
                    float _xscale = ((ringScale.X * i/2) * 0.1f) + 0.01f * i; //controls how wide each individual ring will be
                    if (Projectile.velocity == Vector2.Zero)
                    {
                        _xscale += 0.15f;
                        Projectile.width = 140;
                    }
                    float _yscale = 0.4f; //controls how tall each individual ring will be
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
                    var col = new Color(0.886f, 0.345f, 0.133f, 0.3f); //color
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