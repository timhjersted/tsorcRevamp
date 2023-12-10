using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
    public abstract class RuneterraBlindingLaser : ModProjectile
    {
        public abstract int CooldownType { get; }
        public abstract string SoundPath { get; }
        public abstract int DebuffType { get; }
        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.aiStyle = ProjAIStyleID.SmallFlying;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.light = 0.75f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 3;

            AIType = ProjectileID.Bat;
        }
        public override void OnSpawn(IEntitySource source)
        {
            var player = Main.player[Projectile.owner];
            player.AddBuff(CooldownType, AlienGun.BlindingLaserCooldown * 60);
            Projectile.CritChance += AlienGun.BlindingLaserBonusCritChance;
            player.statMana -= (int)(RuneterraDarts.BaseLaserManaCost * player.manaCost);
            player.ManaEffect(-(int)(RuneterraDarts.BaseLaserManaCost * player.manaCost));
            player.manaRegenDelay = MeleeEdits.ManaDelay / 2;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(new SoundStyle(SoundPath + "BlindingLaserHit") with { Volume = 1f });
            target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerRanger = Main.player[Projectile.owner];
            target.AddBuff(DebuffType, ToxicShot.DebuffDuration * 60);
            target.AddBuff(BuffID.Confused, ToxicShot.DebuffDuration * 60);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= AlienGun.BlindingLaserDmgMult;
            modifiers.FinalDamage.Flat += Math.Min(target.lifeMax * AlienGun.BlindingLaserPercentHPDmg / 100f, AlienGun.BlindingLaserHPDmgCap);
        }
    }
}