using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Shortswords;

namespace tsorcRevamp.Projectiles.Melee.Shortswords
{
    // Shortsword projectiles are handled in a special way with how they draw and damage things
    // The "hitbox" itself is closer to the player, the sprite is centered on it
    // However the interactions with the world will occur offset from this hitbox, closer to the sword's tip (CutTiles, Colliding)
    // Values chosen mostly correspond to Iron Shortword
    public class BarbarousThornBladeProjectile : ModdedShortswordProj
    {
        public override float HitboxWidth => 10f;
        public override float HitboxLength => 12f;

        public override int SpriteWidth => 46;
        public override int SpriteHeight => 46;
        public override int TotalDuration => 16;
        
        public override void PostDraw(Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.BarbarousThornBladeGlowmask];

            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;

            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
            base.PostDraw(lightColor);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 unitVectorTowardsMouse = owner.Center.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * owner.direction) * 7f;
            Projectile Briar1 = Projectile.NewProjectileDirect(Terraria.Entity.GetSource_None(), owner.Center, unitVectorTowardsMouse + new Vector2(0, 2.25f), ModContent.ProjectileType<BarbarousThornBladeBriar>(), BarbarousThornBlade.BriarDmg, hit.Knockback, owner.whoAmI);
            Projectile Briar2 = Projectile.NewProjectileDirect(Terraria.Entity.GetSource_None(), owner.Center, unitVectorTowardsMouse, ModContent.ProjectileType<BarbarousThornBladeBriar>(), BarbarousThornBlade.BriarDmg, hit.Knockback, owner.whoAmI);
            Projectile Briar3 = Projectile.NewProjectileDirect(Terraria.Entity.GetSource_None(), owner.Center, unitVectorTowardsMouse + new Vector2(0, -2.25f), ModContent.ProjectileType<BarbarousThornBladeBriar>(), BarbarousThornBlade.BriarDmg, hit.Knockback, owner.whoAmI);
            Briar1.CritChance = Briar2.CritChance = Briar3.CritChance = Projectile.CritChance;
        }
    }
}
