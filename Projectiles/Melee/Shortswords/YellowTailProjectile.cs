using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.Items.Weapons.Melee.Shortswords;

namespace tsorcRevamp.Projectiles.Melee.Shortswords
{
    // Shortsword projectiles are handled in a special way with how they draw and damage things
    // The "hitbox" itself is closer to the player, the sprite is centered on it
    // However the interactions with the world will occur offset from this hitbox, closer to the sword's tip (CutTiles, Colliding)
    // Values chosen mostly correspond to Iron Shortword
    public class YellowTailProjectile : ModdedShortswordProj
    {
        public override float HitboxWidth => 10f;
        public override float HitboxLength => 10f;

        public override int SpriteWidth => 46;
        public override int SpriteHeight => 46;
        public override int TotalDuration => 16;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 unitVectorTowardsMouse = owner.Center.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * owner.direction) * 5f;
            Projectile Fishbone1 = Projectile.NewProjectileDirect(Terraria.Entity.GetSource_None(), owner.Center, unitVectorTowardsMouse + new Vector2(0, 1.25f), ModContent.ProjectileType<YellowTailFishbone>(), YellowTail.FishboneDmg, hit.Knockback, owner.whoAmI);
            Projectile Fishbone2 = Projectile.NewProjectileDirect(Terraria.Entity.GetSource_None(), owner.Center, unitVectorTowardsMouse + new Vector2(0, -1.25f), ModContent.ProjectileType<YellowTailFishbone>(), YellowTail.FishboneDmg, hit.Knockback, owner.whoAmI);
            Fishbone1.CritChance = Fishbone2.CritChance = Projectile.CritChance;
        }
    }
}
