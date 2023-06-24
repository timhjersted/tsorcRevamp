using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using tsorcRevamp.Buffs.Runeterra.Magic;
using Terraria.Audio;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public class OrbOfSpiritualityCharm : ModProjectile
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
            Projectile.scale = 1.2f;
			Projectile.height = 28; // The height of your projectile
			Projectile.friendly = true; // Deals damage to enemies
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
            player.AddBuff(ModContent.BuffType<OrbOfSpiritualityCharmCooldown>(), OrbOfFlame.FireballCD * 60);
            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/CharmCast") with { Volume = 1f }, player.Center);
            Projectile.velocity *= 0.75f;
        }
        public int frameSpeed = 5;
        public override void AI()
		{
			Player player = Main.player[Projectile.owner];
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.velocity.X < 0) 
            {
                Projectile.rotation -= MathHelper.Pi;
            }
            switch (Projectile.frame)
            {
                case 0:
                    {
                        frameSpeed = 5;
                        break;
                    }
                case 1:
                    {
                        frameSpeed = 4;
                        break;
                    }
                case 2:
                    {
                        frameSpeed = 3;
                        break;
                    }
                case 3:
                    {
                        frameSpeed = 2;
                        break;
                    }
                case 4:
                    {
                        frameSpeed = 2;
                        break;
                    }
                case 5:
                    {
                        frameSpeed = 2;
                        break;
                    }
                case 6:
                    {
                        frameSpeed = 3;
                        break;
                    }
                case 7:
                    {
                        frameSpeed = 4;
                        break;
                    }
            }

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

            Lighting.AddLight(Projectile.Center, Color.Pink.ToVector3() * 1.5f);
            Dust.NewDust(Projectile.Center, 2, 2, DustID.VenomStaff, 0, 0, 150, default, 0.5f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
			target.AddBuff(ModContent.BuffType<Charmed>(), 7 * 60);
            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/CharmHit") with { Volume = 1f }, player.Center);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 2.5f;
            modifiers.FinalDamage.Flat += Math.Min(target.lifeMax / 1000, 450);
        }
    }
}