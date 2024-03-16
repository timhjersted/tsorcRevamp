using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class OldHalberdProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 27f;
        public override float HoldoutRangeMax => 81f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.Blood;
    }

}
