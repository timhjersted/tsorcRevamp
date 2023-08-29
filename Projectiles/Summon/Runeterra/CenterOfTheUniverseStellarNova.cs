using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;

namespace tsorcRevamp.Projectiles.Summon.Runeterra;

public class CenterOfTheUniverseStellarNova : ModProjectile
{
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Projectile.type] = true;
        Main.projFrames[Projectile.type] = 9;
    }
    public override void SetDefaults()
    {
        Projectile.width = 24;
        Projectile.height = 24;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.aiStyle = ProjAIStyleID.NebulaArcanum;
        Projectile.tileCollide = false;
        Projectile.extraUpdates = 3;
        Projectile.timeLeft = 1000;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.penetrate = 3;
    }
    public override void AI()
    {
        Visuals();
    }
    public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
    {
        Terraria.Audio.SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 0.4f, PitchVariance = 0.1f });
        Dust.NewDust(target.Center, 100, 100, DustID.MagicMirror, 0f, 0f, 250, Color.DarkRed, 2.5f);
        if (crit)
        {
            target.AddBuff(ModContent.BuffType<SunburnDebuff>(), 600);
        }
        target.AddBuff(ModContent.BuffType<SunburnDebuff>(), 300);
    }
    private void Visuals()
    {
        int frameSpeed = 5;

        Projectile.frameCounter++;

        if (Projectile.frameCounter >= frameSpeed)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;

            if (Projectile.frame >= Main.projFrames[Projectile.type])
            {
                Projectile.frame = 0;
            }
        }

        Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.78f);
        Dust.NewDust(Projectile.Center, 20, 20, DustID.MagicMirror);
    }
}