using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class DragoonLanceProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 78f;
        public override float HoldoutRangeMax => 234f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.Blood;
    }

}
