using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Melee.Spears;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class AncientBloodLanceProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 62f;
        public override float HoldoutRangeMax => 186f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.Blood;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CrimsonBurn>(), AncientBloodLance.BurnDuration * 60);
        }
    }

}
