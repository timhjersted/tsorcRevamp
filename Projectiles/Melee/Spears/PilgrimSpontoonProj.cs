using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Spears;
using tsorcRevamp.Buffs.Weapons.Melee;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class PilgrimSpontoonProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 52f;
        public override float HoldoutRangeMax => 156f;
        public override float HitboxSize => 1.2f;
        public override float Scale => 1.15f;
        public override int dustID => DustID.UnusedWhiteBluePurple;
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += PilgrimSpontoon.ArmorPen;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // reduce the cooldown if you hit a enemy with the spear 
            Player player = Main.player[Projectile.owner];
            int cooldownBuffIndex = player.FindBuffIndex(ModContent.BuffType<PilgrimSpontoonCooldown>());
            if (cooldownBuffIndex != -1)
            {
                player.buffTime[cooldownBuffIndex] = Math.Max(0, player.buffTime[cooldownBuffIndex] - 30);
            }
        }
    }

}
