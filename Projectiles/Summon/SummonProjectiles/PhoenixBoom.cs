using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.SummonProjectiles;

public class PhoenixBoom : ModProjectile
{

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Projectile.type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.extraUpdates = 100;
        Projectile.timeLeft = 180;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Summon;
    }

    public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
    {
        Dust.NewDust(target.Center, 30, 30, DustID.GoldFlame, 0f, 0f, 150, Color.Red, 1.5f);
        if (crit == true)
        {
            Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, Vector2.Zero, ModContent.ProjectileType<PhoenixBoomCrit>(), Projectile.damage * 2, 1f, Main.myPlayer);
        }
    }

}
public class PhoenixBoomCrit : ModProjectile
{
    public override string Texture => "tsorcRevamp/Projectiles/Summon/SummonProjectiles/PhoenixBoom";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Projectile.type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.extraUpdates = 100;
        Projectile.timeLeft = 180;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Summon;
    }
    public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
    {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 0.4f, PitchVariance = 0.1f });
            Dust.NewDust(target.Center, 100, 100, DustID.FlameBurst, 0f, 0f, 250, Color.DarkRed, 2.5f);
    }
    public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
    {
        crit = true;
    }
}