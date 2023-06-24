using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.DataStructures;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public class OrbOfFlameFireball : ModProjectile
    {
		public override void SetStaticDefaults()
		{
			// These lines facilitate the trail drawing
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
			ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            Main.projFrames[Projectile.type] = 8;
        }

		public override void SetDefaults()
		{
			Projectile.netImportant = true; // This ensures that the projectile is synced when other players join the world.
			Projectile.width = 66; // The width of your projectile
			Projectile.height = 28; // The height of your projectile
			Projectile.friendly = true; // Deals damage to enemies
            Projectile.scale = 1.3f;
			Projectile.penetrate = 1;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.usesLocalNPCImmunity = true; // Used for hit cooldown changes in the ai hook
			Projectile.localNPCHitCooldown = 10; // This facilitates custom hit cooldown logic
			Projectile.tileCollide = false;
			Projectile.aiStyle = -1;

		}

        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            player.AddBuff(ModContent.BuffType<OrbOfFlameFireballCooldown>(), OrbOfFlame.FireballCD * 60);
            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfFlame/FireballCast") with { Volume = 2f }, player.Center);
            Projectile.velocity *= 0.75f;
        }

        public override void AI()
		{
			Player player = Main.player[Projectile.owner];
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.velocity.X < 0)
            {
                Projectile.spriteDirection = -1;
                Projectile.rotation -= MathHelper.Pi;
            }
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

            Lighting.AddLight(Projectile.Center, Color.Firebrick.ToVector3() * 1.5f);
            Dust.NewDust(Projectile.Center, 2, 2, DustID.Torch, 0, 0, 150, default, 0.5f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
			target.AddBuff(ModContent.BuffType<Charmed>(), 5 * 60);
            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfFlame/FireballHit") with { Volume = 6f }, player.Center);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage += OrbOfFlame.FireballDmgMod / 100f;
            modifiers.FinalDamage.Flat += Math.Min(target.lifeMax * OrbOfFlame.FireballHPPercentDmg, OrbOfFlame.FireballHPDmgCap);
        }

        public override bool PreDraw(ref Color lightColor)
		{
			return true;
		}
    }
}