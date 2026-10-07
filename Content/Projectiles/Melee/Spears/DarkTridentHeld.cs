using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Melee.Spears
{
    class DarkTridentHeld : ChargedSpearHeld
    {
        protected override int ThrownType => ModContent.ProjectileType<DarkTridentThrown>();
        protected override int PokeType => ModContent.ProjectileType<DarkTridentPoke>();
        protected override float ReadyHoldoutDistance => 20f; // eyeballed: DarkTridentPoke draws itself pulled back 50px from its start point
        protected override int ChargingDyeItem => ItemID.YellowDye;
        protected override int FullChargeDyeItem => ItemID.SolarDye;

        protected override void SetStats()
        {
            Player owner = Main.LocalPlayer;
            Item bow = owner.inventory[owner.selectedItem];
            StatModifier rangedDamage = owner.GetTotalDamage(DamageClass.Ranged);
            int bowDamage = (int)rangedDamage.ApplyTo(owner.arrowDamage.ApplyTo(bow.damage));
            minDamage = 1;
            maxDamage = bowDamage;
            minVelocity = bow.shootSpeed / 10;
            maxVelocity = bow.shootSpeed;
            chargeRate = (1f / 48f);
            Main.projFrames[Projectile.type] = 1;
            soundtype = SoundID.Item1;
        }

        protected override void PlayFullChargeCue(Player player)
        {
            UsefulFunctions.DustRing(player.Center, 70, DustID.Torch, 60, 18);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, player.Center);
        }
    }
}
