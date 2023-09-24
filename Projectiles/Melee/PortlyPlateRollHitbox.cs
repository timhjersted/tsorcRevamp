using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;


namespace tsorcRevamp.Projectiles.Melee
{
    class PortlyPlateRollHitbox : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = Player.defaultWidth;
            Projectile.height = Player.defaultHeight;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.position = player.Center;
            if (player.GetModPlayer<tsorcRevampPlayer>().PortlyPlateArmor)
            {
                Projectile.timeLeft = 2;
            }
        }

        public override void OnKill(int timeLeft)
        {
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().isDodging)
            {
                return null;
            }
            else
            {
                return false;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
