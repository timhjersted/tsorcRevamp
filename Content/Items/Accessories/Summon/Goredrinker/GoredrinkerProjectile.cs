using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Content.Items.Weapons.Summon.Whips.ModdedWhip;

namespace tsorcRevamp.Content.Items.Accessories.Summon.Goredrinker
{
    public class GoredrinkerProjectile : GlobalProjectile
    {
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            Player player = Main.player[projectile.owner];
            var modPlayer = player.GetModPlayer<GoredrinkerPlayer>();
            var modGlobalProjectile = projectile.GetGlobalProjectile<ModdedWhipGlobalProjectile>();
            if (projectile.friendly)
            {
                if (modPlayer.Equipped && !player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && projectile.DamageType == DamageClass.SummonMeleeSpeed && modPlayer.Ready
                    && ProjectileID.Sets.IsAWhip[projectile.type] && !modGlobalProjectile.IsModded) //Modded whips have this in their code itself because some of them can be charged
                {
                    modPlayer.Swung = true;
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/GoredrinkerSwing") with { Volume = .3f }, player.Center);
                }
            }
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            if (projectile.friendly && !projectile.hostile)
            {
                Player owner = Main.player[projectile.owner];
                var modPlayer = Main.player[projectile.owner].GetModPlayer<GoredrinkerPlayer>();
                if (modPlayer.Equipped && !owner.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && projectile.DamageType == DamageClass.SummonMeleeSpeed && ProjectileID.Sets.IsAWhip[projectile.type] && modPlayer.Swung)
                {
                    owner.AddBuff(ModContent.BuffType<GoredrinkerCooldown>(), GoredrinkerItem.Cooldown * 60);
                    modPlayer.Ready = false;
                    modPlayer.Swung = false;
                }

            }
        }
    }
}

