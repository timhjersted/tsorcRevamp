using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Shortswords
{
    // Shortsword projectiles are handled in a special way with how they draw and damage things
    // The "hitbox" itself is closer to the player, the sprite is centered on it
    // However the interactions with the world will occur offset from this hitbox, closer to the sword's tip (CutTiles, Colliding)
    // Values chosen mostly correspond to Iron Shortword
    public class ClaiomhSolaisProjectile : ModdedShortswordProj
    {
        public override float HitboxWidth => 10f;
        public override float HitboxLength => 16f;

        public override int SpriteWidth => 68;
        public override int SpriteHeight => 68;
        public override int TotalDuration => 16;
    }
}
