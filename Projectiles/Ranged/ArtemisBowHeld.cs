using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Ranged
{
    class ArtemisBowHeld : ChargedBowHeld
    {
        protected override void SetStats()
        {
            Player owner = Main.LocalPlayer;
            Item bow = owner.inventory[owner.selectedItem];
            StatModifier rangedDamage = owner.GetTotalDamage(DamageClass.Ranged);
            int bowDamage = (int)rangedDamage.ApplyTo(owner.arrowDamage.ApplyTo(bow.damage));
            minDamage = bowDamage / 10;
            maxDamage = bowDamage;
            minVelocity = bow.shootSpeed / 10;
            maxVelocity = bow.shootSpeed;
            chargeRate = (1f / bow.useTime);
            DrawOffsetX = -50;
            DrawOriginOffsetY = -35;
            Main.projFrames[Projectile.type] = 7;
        }

        protected override void Shoot()
        {
            Player player = Main.player[Projectile.owner];
            if (player.whoAmI != Main.myPlayer)
                return;
            float velocity = LerpFloat(minVelocity, maxVelocity, charge);
            Vector2 inaccuracy = (aimVector * velocity).RotatedByRandom(MathHelper.ToRadians(18f - (15f * charge)));
            int damage = (int)LerpFloat(minDamage, maxDamage, charge) + Projectile.damage;
            Projectile.NewProjectile(player.GetSource_ItemUse(player.inventory[player.selectedItem]), player.Center, inaccuracy, ammoType, damage, Projectile.knockBack, Projectile.owner);
        }
    }
}
