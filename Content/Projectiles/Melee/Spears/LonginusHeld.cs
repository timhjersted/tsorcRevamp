using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Melee.Spears
{
    class LonginusHeld : ChargedSpearHeld
    {
        protected override int ThrownType => ModContent.ProjectileType<LonginusThrown>();
        protected override int PokeType => ModContent.ProjectileType<LonginusPoke>();
        protected override float ReadyHoldoutDistance => 100f; // LonginusPoke.HoldoutRangeMin
        protected override int ChargingDyeItem => ItemID.RedDye;
        protected override int FullChargeDyeItem => ItemID.RedDye;

        protected override void SetStats()
        {
            Player owner = Main.LocalPlayer;
            Item bow = owner.inventory[owner.selectedItem];
            StatModifier meleeDamage = owner.GetTotalDamage(DamageClass.Melee);
            int bowDamage = (int)meleeDamage.ApplyTo(owner.arrowDamage.ApplyTo(bow.damage));
            minDamage = 1;
            maxDamage = bowDamage;
            minVelocity = bow.shootSpeed / 10;
            maxVelocity = bow.shootSpeed;
            chargeRate = (1f / 85f);
            Main.projFrames[Projectile.type] = 1;
            soundtype = SoundID.Item1;
        }

        protected override void PlayFullChargeCue(Player player)
        {
            UsefulFunctions.DustRing(player.Center, 70, 219, 70, 25);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f }, player.Center);
        }
    }
}
