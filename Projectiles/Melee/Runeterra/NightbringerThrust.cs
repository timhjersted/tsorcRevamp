using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;
using tsorcRevamp.Projectiles.Melee.Shortswords;

namespace tsorcRevamp.Projectiles.Melee.Runeterra
{
    public class NightbringerThrust : ModdedShortswordProj
    {
        public override float HitboxWidth => 20f;
        public override float HitboxLength => 21.5f;

        public override int SpriteWidth => 186;
        public override int SpriteHeight => 220;
        public override int TotalDuration => 32;
        public override float SetDefaultScale()
        {
            return Nightbringer.BaseScale;
        }
        public override void Initialization(Player player)
        {
            Projectile.scale = player.GetAdjustedItemScale(player.HeldItem) * 1.15f;
            Projectile.Resize((int)(Projectile.width / SetDefaultScale() * Projectile.scale), (int)(Projectile.height / SetDefaultScale() * Projectile.scale));
            CollisionWidth *= Projectile.scale;
            Projectile.velocity /= player.GetTotalAttackSpeed(DamageClass.Melee);
        }

        public override void SetVisualOffsets()
        {
            base.SetVisualOffsets();
            Player player = Main.player[Projectile.owner];
            Vector2 playerCenter = player.RotatedRelativePoint(player.MountedCenter, reverseRotation: false, addGfxOffY: false);
            Projectile.Center = playerCenter + Projectile.velocity * (Timer + 10f);
            
            Projectile.frame = (int)((Timer / 28f) * 11f);
            if (Timer > 28f)
            {
                Projectile.frame = 0;
            }
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 1.4f);
        }
        public bool Hit = false;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 11;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 2;
        }
        public static Texture2D texture;
        public static Texture2D glowTexture;
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;

            if (Main.player[Projectile.owner].direction == 1)
            {
            }
            else
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            texture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad);
            glowTexture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture + "Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            int frameHeight = ((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]).Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), sourceRectangle, lightColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);


            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            if (!Hit)
            {
                Hit = true;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/ThrustHit") with { Volume = 0.75f });
            }
        }
        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            player.itemAnimation = 0;
            player.itemTime = 0;
            if (Hit)
            {
                player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks += 1;
                if (Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/TornadoReady") with { Volume = 0.75f });
                }
            }
        }
    }
}
