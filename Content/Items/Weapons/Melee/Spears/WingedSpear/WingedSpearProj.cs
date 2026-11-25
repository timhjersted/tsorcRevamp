using Terraria.ID;
using tsorcRevamp.Content.Projectiles.Melee;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears.WingedSpear
{
    class WingedSpearProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 42f;
        public override float HoldoutRangeMax => 126f;
        public override float HitboxSize => 1;
        public override float Scale => 1.1f;
        public override int dustID => DustID.Cloud;
    }

}
