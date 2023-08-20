using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;

namespace tsorcRevamp.Projectiles.Summon.Archer
{
    internal class ArcherToken : ModProjectile 
    {
        public override void SetStaticDefaults() 
        {
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.alpha = 255;
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (player.dead || !player.active) {
                player.ClearBuff(ModContent.BuffType<NondescriptOwlBuff>());
            }

            if (player.HasBuff(ModContent.BuffType<NondescriptOwlBuff>())) {
                Projectile.timeLeft = 2;
            }

            Projectile.Center = player.Center;
            //Main.NewText("token");
        }

        public override bool? CanDamage() {
            return false;
        }
    }
}
