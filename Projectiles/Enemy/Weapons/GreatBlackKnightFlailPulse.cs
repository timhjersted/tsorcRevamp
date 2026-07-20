using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Invisible wide-radius pulse spawned periodically by GreatBlackKnightFlail while it's out. Tracks the flail
    /// head (ai[0] = the flail projectile's whoAmI) and applies a curse debuff in an AOE around it, with a dark
    /// dust ring for the visual read — modeled on the player Berserker's Nightmare flail's BerserkerNightmareAura.
    /// </summary>
    public class GreatBlackKnightFlailPulse : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 110;
            Projectile.height = 110;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 3;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.hide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            int ballID = (int)Projectile.ai[0];
            if (ballID < 0 || ballID >= Main.maxProjectiles || !Main.projectile[ballID].active)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = Main.projectile[ballID].Center;

            for (int i = 0; i < 24; i++)
            {
                float angle = MathHelper.TwoPi * i / 24f;
                Vector2 pos = Projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 55f;
                Dust d = Dust.NewDustPerfect(pos, DustID.ShadowbeamStaff, Vector2.Zero, 100, Color.DarkSlateGray, 1.2f);
                d.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 300, false);
            target.AddBuff(BuffID.Frostburn, 6 * 60);
        }
    }
}
