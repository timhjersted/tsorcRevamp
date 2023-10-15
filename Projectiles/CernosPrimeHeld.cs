using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class CernosPrimeHeld : ChargedBowHeld
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
            DrawOffsetX = -30;
            DrawOriginOffsetY = -35;
            Main.projFrames[Projectile.type] = 7;
        }

        protected override void Shoot()
        {
            Player player = Main.player[Projectile.owner];
            if (player.whoAmI != Main.myPlayer)
                return;
            int arrow = ammoType; //directly using ammoType in the NewProjectile call eats it on the first shot, making subsequent shots empty

            for (int i = 0; i < 3; i++)
            {
                float velocity = LerpFloat(minVelocity, maxVelocity, charge);
                int damage = (int)LerpFloat(minDamage, maxDamage, charge);
                Vector2 inaccuracy = (aimVector * velocity).RotatedByRandom(MathHelper.ToRadians(18f - (15f * charge)));
                Projectile.NewProjectile(player.GetSource_ItemUse(player.inventory[player.selectedItem]), player.Center, inaccuracy, arrow, damage, Projectile.knockBack, Projectile.owner);
            }

        }
    }
}
