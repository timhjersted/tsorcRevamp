using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class CobaltHalberdProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 66f;
        public override float HoldoutRangeMax => 198f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.Blood;
    }

}
