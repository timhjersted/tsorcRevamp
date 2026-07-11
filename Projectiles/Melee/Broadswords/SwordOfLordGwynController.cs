using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Items.Armors;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    class SwordOfLordGwynController : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int TapWindow = 14;
        const int NovaChargeTicks = 54;
        const int DashTicks = 18;

        int Mode => (int)Projectile.ai[0]; //0 = cursor high, 1 = cursor low
        float Timer => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.hide = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = player.Center;
            Projectile.timeLeft = 60;
            Projectile.localAI[0]++;
            bool channeling = player.channel;

            if (channeling)
            {
                player.heldProj = Projectile.whoAmI;
                player.itemTime = 2;
                player.itemAnimation = 2;
                if (Main.myPlayer == Projectile.owner)
                {
                    player.ChangeDir(Main.MouseWorld.X > player.Center.X ? 1 : -1);
                }
            }

            if (Timer <= TapWindow)
            {
                if (!channeling)
                {
                    if (Main.myPlayer == Projectile.owner)
                    {
                        CastTap(player);
                    }
                    Projectile.Kill();
                }
                return;
            }

            if (Mode == 0)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    CastSunlightSpear(player);
                }
                Projectile.Kill();
            }
            else
            {
                RunCinderNovaCharge(player, channeling);
            }
        }

        void CastTap(Player player)
        {
            if (Mode == 0)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.25f }, player.Center);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                    ModContent.ProjectileType<SwordOfLordGwynSlash>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, player.direction);
            }
            else
            {
                CastRisingDash(player);
            }
        }

        void CastSunlightSpear(Player player)
        {
            if (!player.CheckMana(SwordOfLordGwyn.SpearManaCost, true))
            {
                return;
            }

            player.manaRegenDelay = 180;
            Vector2 origin = player.Center + new Vector2(player.direction * 28f, -12f);
            Vector2 aim = (Main.MouseWorld - origin).SafeNormalize(Vector2.UnitX * player.direction);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.85f, Pitch = 0.1f }, origin);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, aim * 16f,
                ModContent.ProjectileType<SwordOfLordGwynSunlightSpear>(), (int)(Projectile.damage * 0.95f), Projectile.knockBack * 0.8f, Projectile.owner, player.GetModPlayer<LordGwynSetPlayer>().AboveHalfMana ? 1f : 0f);
        }

        void CastRisingDash(Player player)
        {
            if (Main.myPlayer == Projectile.owner && !player.CheckMana(SwordOfLordGwyn.DashManaCost, true))
            {
                return;
            }

            player.manaRegenDelay = 180;
            bool empowered = player.GetModPlayer<LordGwynSetPlayer>().AboveHalfMana;
            player.direction = Main.MouseWorld.X > player.Center.X ? 1 : -1;
            player.velocity = new Vector2(player.direction * 17f, -8.25f);
            player.immune = true;
            player.immuneTime = 18;
            player.ResetMeleeHitCooldowns();

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.75f, Pitch = -0.35f }, player.Center);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                ModContent.ProjectileType<SwordOfLordGwynSlash>(), (int)(Projectile.damage * (empowered ? 1.55f : 1.35f)), Projectile.knockBack * (empowered ? 2.1f : 1.8f), Projectile.owner, 1f, player.direction);

            for (int i = 0; i < (empowered ? DashTicks + 12 : DashTicks); i += 3)
            {
                Vector2 trailPos = player.Center - new Vector2(player.direction * i * 11f, 0f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), trailPos, Vector2.Zero,
                    ModContent.ProjectileType<SwordOfLordGwynCinderTrail>(), (int)(Projectile.damage * (empowered ? 0.4f : 0.28f)), Projectile.knockBack * 0.35f, Projectile.owner);
            }

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI);
            }
        }

        void RunCinderNovaCharge(Player player, bool channeling)
        {
            float charge = Timer - TapWindow;
            if (!channeling && charge < NovaChargeTicks)
            {
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, DustID.GoldFlame, 0f, -1f, 100, default, 1.2f);
                    Main.dust[dust].noGravity = true;
                }
                Projectile.Kill();
                return;
            }

            float progress = MathHelper.Clamp(charge / NovaChargeTicks, 0f, 1f);
            for (int i = 0; i < 2 + (int)(progress * 4f); i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = MathHelper.Lerp(110f, 20f, progress) + Main.rand.NextFloat(18f);
                Vector2 pos = player.Center + angle.ToRotationVector2() * radius;
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, 0f, 70, default, 1.25f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = (player.Center - pos) * 0.08f;
            }
            Lighting.AddLight(player.Center, 0.8f * progress, 0.45f * progress, 0.15f * progress);

            if (charge >= NovaChargeTicks)
            {
                if (Main.myPlayer == Projectile.owner && player.CheckMana(SwordOfLordGwyn.NovaManaCost, true))
                {
                    player.manaRegenDelay = 180;
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.55f }, player.Center);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                        ModContent.ProjectileType<SwordOfLordGwynCinderNova>(), (int)(Projectile.damage * 1.65f), Projectile.knockBack * 1.6f, Projectile.owner, player.GetModPlayer<LordGwynSetPlayer>().AboveHalfMana ? 430f : 360f);
                }
                Projectile.Kill();
            }
        }
    }
}
