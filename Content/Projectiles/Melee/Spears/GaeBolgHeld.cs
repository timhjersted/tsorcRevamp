using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Melee.Spears
{
    class GaeBolgHeld : ChargedSpearHeld
    {
        protected override int ThrownType => ModContent.ProjectileType<GaeBolgThrown>();
        protected override int PokeType => ModContent.ProjectileType<GaeBolgPoke>();
        protected override float ReadyHoldoutDistance => 75f; // GaeBolgPoke.HoldoutRangeMin
        protected override int ChargingDyeItem => ItemID.SkyBlueDye;
        protected override int FullChargeDyeItem => ItemID.StardustDye;

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
            chargeRate = (1f / 44f);
            Main.projFrames[Projectile.type] = 1;
            soundtype = SoundID.Item1;
        }

        protected override void PlayFullChargeCue(Player player)
        {
            UsefulFunctions.DustRing(player.Center, 70, 15, 60, 23);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.6f }, player.Center);
        }
    }
}
