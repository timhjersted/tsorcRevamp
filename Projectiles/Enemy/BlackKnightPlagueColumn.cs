using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// A column of black death the Black Knight seals into the ground: a floor telegraph, then a
    /// 2x15-tile pillar of plague that stands for seven seconds.
    /// </summary>
    /// <remarks>
    /// The whole point is that it is placed, not aimed — it answers a player who stands still and it
    /// progressively takes the arena away. It is therefore ALWAYS telegraphed on the floor first, even for
    /// the follow-up pair that gets no caster animation: a column that erupts under you with no warning is
    /// not zoning, it is a tax.
    ///
    /// `ai[0]` = floor-telegraph ticks before eruption. `ai[1]` = damage carried through the delay (the
    /// projectile spawns with 0 damage so it cannot hurt anyone while it is only a warning on the ground).
    /// </remarks>
    public class BlackKnightPlagueColumn : ModProjectile
    {
        public const int ColumnWidth = 32;      // 2 tiles
        public const int ColumnHeight = 240;    // 15 tiles
        public const int ActiveTicks = 7 * 60;  // stands for 7s once erupted
        private const int EruptionRampTicks = 14;
        private const int CurseBuildupPerTick = 2;
        private const int BuffRefreshTicks = 120;

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        private int TelegraphTicks => (int)Projectile.ai[0];
        private int CarriedDamage => (int)Projectile.ai[1];
        private bool Erupted => Projectile.localAI[0] >= TelegraphTicks;
        private float EruptProgress => MathHelper.Clamp(
            (Projectile.localAI[0] - TelegraphTicks) / (float)EruptionRampTicks, 0f, 1f);

        public override void SetDefaults()
        {
            Projectile.width = ColumnWidth;
            Projectile.height = ColumnHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = ActiveTicks;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            // Anchored: it is sealed into a spot on the floor and does not drift.
            Projectile.velocity = Vector2.Zero;

            if (!Erupted)
            {
                // Hold timeLeft so the seven-second life is measured from ERUPTION, not from placement —
                // otherwise the delayed pair would stand for three seconds less than the first.
                Projectile.timeLeft = ActiveTicks;
                Projectile.damage = 0;
                if (!Main.dedServ)
                {
                    DrawFloorTelegraph();
                }
                return;
            }

            if (Projectile.damage == 0)
            {
                Projectile.damage = CarriedDamage;
                if (Projectile.localAI[0] == TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item68 with { Volume = 0.7f, Pitch = -0.5f }, Projectile.Center);
                }
            }

            if (!Main.dedServ)
            {
                DrawColumn();
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ApplyCurse();
            }
        }

        /// <summary>Ring of motes on the ground marking exactly the footprint that is about to erupt.</summary>
        private void DrawFloorTelegraph()
        {
            float pulse = 0.5f + 0.5f * (float)System.Math.Sin(Projectile.localAI[0] * 0.22f);
            int count = 2 + (int)(pulse * 3f);
            for (int i = 0; i < count; i++)
            {
                Vector2 position = Projectile.Bottom + new Vector2(
                    Main.rand.NextFloat(-ColumnWidth * 0.5f, ColumnWidth * 0.5f), Main.rand.NextFloat(-3f, 3f));
                Dust mote = Dust.NewDustPerfect(position, DustID.ShadowbeamStaff,
                    new Vector2(0f, -Main.rand.NextFloat(0.6f, 1.9f) * (0.4f + pulse)), 170,
                    new Color(96, 34, 148), Main.rand.NextFloat(0.9f, 1.5f));
                mote.noGravity = true;
            }
        }

        private void DrawColumn()
        {
            float fade = MathHelper.Clamp(Projectile.timeLeft / 45f, 0f, 1f);
            float ramp = EruptProgress;
            int count = (int)MathHelper.Lerp(6f, 14f, ramp) ;
            for (int i = 0; i < count; i++)
            {
                float height = Main.rand.NextFloat(0f, ColumnHeight * ramp);
                Vector2 position = Projectile.Bottom + new Vector2(
                    Main.rand.NextFloat(-ColumnWidth * 0.55f, ColumnWidth * 0.55f), -height);
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.8f, 2.6f));

                bool ember = Main.rand.NextBool(3);
                Dust dust = Dust.NewDustPerfect(position, ember ? DustID.PurpleTorch : DustID.Smoke, velocity, 180,
                    ember ? new Color(132, 48, 186) : new Color(14, 9, 20),
                    Main.rand.NextFloat(1.2f, 2.3f) * fade);
                dust.noGravity = true;
                dust.fadeIn = 0.6f;
            }
            Lighting.AddLight(Projectile.Center, new Vector3(0.32f, 0.10f, 0.44f) * fade);
        }

        private void ApplyCurse()
        {
            int curseBuff = ModContent.BuffType<CurseBuildup>();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || !player.Hitbox.Intersects(Projectile.Hitbox))
                {
                    continue;
                }

                player.GetModPlayer<tsorcRevampPlayer>().CurseLevel += CurseBuildupPerTick;
                int buffIndex = player.FindBuffIndex(curseBuff);
                if (buffIndex == -1)
                {
                    player.AddBuff(curseBuff, BuffRefreshTicks, false);
                }
                else if (player.buffTime[buffIndex] < BuffRefreshTicks)
                {
                    player.buffTime[buffIndex] = BuffRefreshTicks;
                }
            }
        }

        public override bool? CanDamage() => Erupted;

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
