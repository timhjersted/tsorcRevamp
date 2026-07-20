using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Items
{
    /// <summary>Lets ordinary and reworked melee item hitboxes cut virtual snare nodes without
    /// changing world tiles. The net itself rate-limits cuts so one animation cannot erase every
    /// strand merely because UseItemHitbox is evaluated on several active frames.</summary>
    public class PuppetSnareCutGlobalItem : GlobalItem
    {
        public override void UseItemHitbox(
            Item item,
            Player player,
            ref Rectangle hitbox,
            ref bool noHitbox)
        {
            if (player.itemAnimation <= 0 || item.damage <= 0
                || !item.DamageType.CountsAsClass(DamageClass.Melee)
                || (item.useStyle != ItemUseStyleID.Swing && item.useStyle != ItemUseStyleID.Thrust))
            {
                return;
            }

            int snareType = ModContent.ProjectileType<PuppetSnareNet>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != snareType
                    || projectile.ModProjectile is not PuppetSnareNet snare)
                {
                    continue;
                }
                snare.TryCutNodes(hitbox, player);
            }
        }
    }
}
