using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.AlienGun;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.OmegaSquadRifle;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.ToxicShot;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Bases;

    public class RuneterraDartsGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool AppliedAlready;

        public override void AI(Projectile projectile)
        {
            Player player = Main.player[projectile.owner];
            var modPlayer = player.GetModPlayer<RuneterraDartsPlayer>();
            bool isPoisonDart = projectile.type == ProjectileID.PoisonDartBlowgun;
            bool holdingWeapon = modPlayer.ScoutsHeldItem || modPlayer.AlienScoutsHeldItem || modPlayer.OmegaScoutsHeldItem;
            if (!AppliedAlready && isPoisonDart && holdingWeapon)
            {
                projectile.penetrate += RuneterraDarts.PoisonDartPierceBonus;
                projectile.usesLocalNPCImmunity = true;
                projectile.localNPCHitCooldown = -1;
                AppliedAlready = true;
            }
        }
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[projectile.owner];
            var modPlayer = player.GetModPlayer<RuneterraDartsPlayer>();
            var globalNPC = target.GetGlobalNPC<RuneterraDartsNPC>();
            bool isPoisonDart = projectile.type == ProjectileID.PoisonDartBlowgun;
            if (isPoisonDart)
            {
                if (modPlayer.ScoutsHeldItem)
                {
                    if (hit.Crit)
                    {
                        SoundEngine.PlaySound(new SoundStyle( UsefulFunctions.RefactorableFilepath(typeof(ToxicShotSound)) + "_ShotCrit") with { Volume = 0.5f }, target.Center);
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(ToxicShotSound)) + "_ShotHit") with { Volume = 0.5f }, target.Center);
                    } 
                    globalNPC.LastHitter = Main.player[projectile.owner];
                    target.AddBuff(ModContent.BuffType<VenomDebuff>(), 2 * 60);
                } 
                else if (modPlayer.AlienScoutsHeldItem)
                {
                    if (hit.Crit)
                    {
                        SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(AlienGunSound)) + "_ShotCrit") with { Volume = 0.5f }, target.Center);
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(AlienGunSound)) + "_ShotHit") with { Volume = 0.5f }, target.Center);
                    }
                    globalNPC.LastHitter = Main.player[projectile.owner];
                    target.AddBuff(ModContent.BuffType<ElectrifiedDebuff>(), 2 * 60);
                }
                else if (modPlayer.OmegaScoutsHeldItem)
                {
                    if (hit.Crit)
                    {
                        SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(OmegaSquadRifleSound)) + "_ShotCrit") with { Volume = 0.5f }, target.Center);
                    }
                    else
                    {
                        SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(OmegaSquadRifleSound)) + "_ShotHit") with { Volume = 0.5f }, target.Center);
                    }
                    globalNPC.LastHitter = Main.player[projectile.owner];
                    target.AddBuff(ModContent.BuffType<IrradiatedDebuff>(), 2 * 60);
                }
            }
        }
    }