using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    ///<summary>
    ///Heart of Winter's held brain: spawned on every use, watches whether the button is TAPPED
    ///(released within 18 ticks) or HELD, and routes to the right spell. ai[0] = 0 for cursor high
    ///(tap: icicle trio / hold: frost breath channel), 1 for cursor low (tap: cursor ground
    ///spikes / hold: charged freeze ring). Deals no damage itself — children inherit its damage.
    ///</summary>
    class HeartOfWinterController : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int TapWindow = 18;   //released before this = the tap spell
        const int ChargeTicks = 60; //cursor-low hold: charge time after the window before the nova
        const int BreathManaCost = 6;   //per 9 ticks of cursor-high hold channel
        const int NovaManaCost = 20;    //surcharge on releasing the freeze ring

        int Mode => (int)Projectile.ai[0]; //0 = cursor high, 1 = cursor low
        bool UsesRightClick => Projectile.ai[1] == 1f;
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
            Projectile.DamageType = DamageClass.Magic;
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
            bool channeling = UsesRightClick ? player.controlUseTile : player.channel;

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

            //Within the tap window: releasing casts the tap spell
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
                RunBreathChannel(player, channeling);
            }
            else
            {
                RunNovaCharge(player, channeling);
            }
        }

        #region Tap spells

        void CastTap(Player player)
        {
            if (Mode == 0)
            {
                //Icicle Crown: three icicles form overhead and lance at the cursor in sequence
                SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.8f, Pitch = 0.2f }, player.Center);
                Span<Vector2> offsets = stackalloc Vector2[] { new Vector2(-30f, -46f), new Vector2(0f, -56f), new Vector2(30f, -46f) };
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = player.Center + offsets[i];
                    Vector2 aim = (Main.MouseWorld - pos).SafeNormalize(Vector2.UnitX * player.direction);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, aim,
                        ModContent.ProjectileType<TomeIcicle>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 4f + i * 5f);
                }
            }
            else
            {
                //Rimefang: ground spikes erupt at the cursor (clamped to ~40 tiles of reach)
                Vector2 cursor = Main.MouseWorld;
                if (Vector2.Distance(cursor, player.Center) > 640f)
                {
                    cursor = player.Center + (cursor - player.Center).SafeNormalize(Vector2.UnitX) * 640f;
                }
                float groundY = FindGroundY(cursor - new Vector2(0f, 32f), 20);
                if (groundY < 0f)
                {
                    //No floor there — fizzle read so the whiff is legible
                    for (int i = 0; i < 6; i++)
                    {
                        int dust = Dust.NewDust(cursor - new Vector2(8f, 8f), 16, 16, DustID.Frost, 0f, 0f, 120, default, 1f);
                        Main.dust[dust].noGravity = true;
                    }
                    return;
                }
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = -0.2f }, cursor);
                for (int i = -1; i <= 1; i++)
                {
                    float x = cursor.X + i * 26f;
                    float colGround = FindGroundY(new Vector2(x, groundY - 40f), 8);
                    if (colGround < 0f)
                    {
                        continue;
                    }
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), new Vector2(x, colGround - 27f), Vector2.Zero,
                        ModContent.ProjectileType<TomeIceSpike>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 6f + (i + 1) * 4f, 1f);
                }
            }
        }

        #endregion

        #region Hold spells

        void RunBreathChannel(Player player, bool channeling)
        {
            if (!channeling)
            {
                Projectile.Kill();
                return;
            }
            if (Main.myPlayer == Projectile.owner)
            {
                if (Timer % 9 == 0 && !player.CheckMana(BreathManaCost, true))
                {
                    Projectile.Kill();
                    return;
                }
                if (Timer % 3 == 0)
                {
                    Vector2 mouth = player.Center + new Vector2(player.direction * 14f, -4f);
                    Vector2 aim = (Main.MouseWorld - mouth).SafeNormalize(Vector2.UnitX * player.direction);
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 vel = aim.RotatedBy(Main.rand.NextFloat(-0.12f, 0.12f)) * (7.5f + Main.rand.NextFloat(-1f, 1f));
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), mouth, vel,
                            ModContent.ProjectileType<TomeFrostPuff>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack * 0.3f, Projectile.owner);
                    }
                }
            }
            if (Timer % 20 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.35f, Pitch = -0.3f }, player.Center);
            }
        }

        void RunNovaCharge(Player player, bool channeling)
        {
            float charge = Timer - TapWindow;
            if (!channeling && charge < ChargeTicks)
            {
                //Let go early: the gathered cold disperses
                for (int i = 0; i < 8; i++)
                {
                    int dust = Dust.NewDust(player.position, player.width, player.height, DustID.Frost, 0f, -1f, 130, default, 1f);
                    Main.dust[dust].noGravity = true;
                }
                Projectile.Kill();
                return;
            }
            //Converging blizzard while charging — same read as the boss's channel
            float progress = MathHelper.Min(1f, charge / ChargeTicks);
            int count = 1 + (int)(progress * 3f);
            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = MathHelper.Lerp(90f, 15f, progress) + Main.rand.NextFloat(12f);
                Vector2 pos = player.Center + angle.ToRotationVector2() * radius;
                int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, 0f, 100, default, 1.1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = (player.Center - pos) * 0.08f;
            }
            Lighting.AddLight(player.Center, 0.2f * progress, 0.35f * progress, 0.5f * progress);

            if (charge >= ChargeTicks)
            {
                if (Main.myPlayer == Projectile.owner && player.CheckMana(NovaManaCost, true))
                {
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.9f, Pitch = -0.6f }, player.Center);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                        ModContent.ProjectileType<TomeFreezeRing>(), (int)(Projectile.damage * 1.5f), Projectile.knockBack * 1.5f, Projectile.owner, 260f);
                }
                Projectile.Kill();
            }
        }

        #endregion

        static float FindGroundY(Vector2 worldPos, int maxTilesDown)
        {
            int tx = (int)(worldPos.X / 16f);
            int ty = (int)(worldPos.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5)
            {
                return -1f;
            }
            for (int d = 0; d <= maxTilesDown; d++)
            {
                int y = ty + d;
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }
                Tile tile = Main.tile[tx, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return y * 16f;
                }
            }
            return -1f;
        }
    }
}
