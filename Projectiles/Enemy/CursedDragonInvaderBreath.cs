using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// The Cursed Dragon invader's fire breath.  Uses the same orange/red flame look and flamethrower
    /// AI (aiStyle 23, no AIType override) as <see cref="FireBreath"/> — the projectile the Ancient Demon
    /// and Omnir's Massacre breathe — but adds the dragon's curse + burn on hit.
    /// </summary>
    public class CursedDragonInvaderBreath : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/FireBreath";

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 20;
            Projectile.aiStyle = 23;   // flamethrower flame — orange/red, decelerates and fades
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.light = 1f;
            Projectile.penetrate = 3;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 1f;
        }

        public override bool PreKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.1f, Pitch = -0.2f }, Projectile.Center); // flamethrower puff
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // On Fire for 5 s on every hit.  Curse buildup is gated 1-in-3 — the breath fires a stream of
            // many penetrating projectiles, so an unconditional 36000-tick curse stack would ramp absurdly.
            target.AddBuff(BuffID.OnFire, 300);
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(ModContent.BuffType<PowerfulCurseBuildup>(), 36000);
            }
        }
    }
}
