using Terraria.ID;
using tsorcRevamp.Content.Projectiles.Melee;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears.OldHalberd
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