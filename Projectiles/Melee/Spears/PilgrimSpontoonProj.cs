using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Spears;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class PilgrimSpontoonProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 52f;
        public override float HoldoutRangeMax => 156f;
        public override float HitboxSize => 1.1f;
        public override float Scale => 1;
        public override int dustID => DustID.UnusedWhiteBluePurple;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += PilgrimSpontoon.ArmorPen;
        }
    }

}
