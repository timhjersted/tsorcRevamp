using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Summon.Whips;
using tsorcRevamp.NPCs;
using tsorcRevamp.Utilities;
using WitchkingBoss = tsorcRevamp.NPCs.Bosses.SuperHardMode.Witchking;

namespace tsorcRevamp.Content.Projectiles.Enemy.Witchking
{
    /// <summary>
    /// Grond's Toll, the Witchking's phase-2 signature. The mace whirls overhead on its chain, speeding
    /// up across three turns while Blight smoke thickens on the head (0–50); at tick 30 the strike point locks — the
    /// floor under the target, at most 12 tiles out — and a violet marker burns on it until the slam. Ticks 50–58 the
    /// head drops onto the mark (the only damaging window); on 58 two void shockwaves run along the floor both ways
    /// (jumpable) and a Blight mire is left on the spot. 58–128 the mace drags back to the hand (the punish window).
    /// Not rollable by design: the slam is a positional test. The Witchking's pose beats read SlamStartTick/ImpactTick.
    /// ai[0] = owner whoAmI (an IEnemyWhipOwner: the hand anchor), ai[1] = target player. Strike point = velocity
    /// (ShouldUpdatePosition is false), synced once by the server at the lock.
    /// Drawn with the player whip's sheet (intentional sharing): handle, 18px links, mace head.
    /// </summary>
    public class WitchkingGrondMace : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(WitchkingMaceProjectile));

        public const int AimLockTick = 30;
        public const int SlamStartTick = 50;
        public const int ImpactTick = 58;
        public const int TotalTicks = 128;
        private const float OrbitRadius = 70f;
        private const float OrbitLift = 50f;
        private const float OrbitTurns = 3f;
        private const float MaxStrikeDistance = 12f * 16f;
        private const int FloorScanTiles = 30;
        private const float HeadHitboxSize = 40f;

        private static readonly Rectangle HandleFrame = new Rectangle(0, 0, 30, 36);
        private static readonly Rectangle LinkFrame = new Rectangle(0, 48, 30, 18);
        private static readonly Rectangle HeadFrame = new Rectangle(0, 70, 30, 50);

        private int Age => (int)Projectile.localAI[0];
        private Vector2 StrikePoint => Projectile.velocity;

        // The head's position at the end of the whirl, kept for the slam's start.
        private Vector2 _orbitEnd;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)HeadHitboxSize;
            Projectile.height = (int)HeadHitboxSize;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks + 5;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Age >= SlamStartTick && Age <= ImpactTick + 2 ? null : false;

        public override void AI()
        {
            if (!TryGetOwner(out NPC owner, out IEnemyWhipOwner whipOwner) || Age >= TotalTicks)
            {
                Projectile.Kill();
                return;
            }

            Projectile.localAI[0]++;
            Vector2 anchor = whipOwner.WhipAnchor;

            if (Age == AimLockTick)
            {
                LockStrikePoint(owner);
            }

            Vector2 head;
            if (Age < SlamStartTick)
            {
                // Accelerating whirl overhead: angle grows with progress^1.5, so the three turns speed up.
                float progress = Age / (float)SlamStartTick;
                float angle = owner.direction * OrbitTurns * MathHelper.TwoPi * MathF.Pow(progress, 1.5f) - MathHelper.PiOver2;
                Vector2 orbitCenter = anchor - new Vector2(0f, OrbitLift);
                head = orbitCenter + angle.ToRotationVector2() * OrbitRadius;
                _orbitEnd = head;
            }
            else if (Age < ImpactTick)
            {
                // Accelerating drop onto the marked floor.
                float slam = (Age - SlamStartTick) / (float)(ImpactTick - SlamStartTick);
                head = Vector2.Lerp(_orbitEnd, StrikePoint, slam * slam);
            }
            else
            {
                float drag = MathHelper.SmoothStep(0f, 1f, (Age - ImpactTick) / (float)(TotalTicks - ImpactTick));
                head = Vector2.Lerp(StrikePoint, anchor, drag);
            }

            Projectile.Center = head;

            if (Age == ImpactTick)
            {
                Impact();
            }

            SpawnTellDust(head);
        }

        // Server picks the floor under the target, at most 12 tiles out from the Witchking; clients compute the same
        // and then adopt the server's sync.
        private void LockStrikePoint(NPC owner)
        {
            int targetIndex = (int)Projectile.ai[1];
            Player target = Main.player[Math.Clamp(targetIndex, 0, Main.maxPlayers - 1)];
            float offsetX = MathHelper.Clamp(target.Center.X - owner.Center.X, -MaxStrikeDistance, MaxStrikeDistance);
            float strikeX = owner.Center.X + offsetX;

            int tileX = (int)(strikeX / 16f);
            int startY = (int)(target.Center.Y / 16f);
            float floorY = owner.Bottom.Y;
            for (int y = startY; y < startY + FloorScanTiles; y++)
            {
                if (WorldGen.SolidTile(tileX, y))
                {
                    floorY = y * 16f;
                    break;
                }
            }

            Projectile.velocity = new Vector2(strikeX, floorY);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.netUpdate = true;
            }
        }

        private void Impact()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item70 with { Volume = 1f, Pitch = -0.6f }, StrikePoint);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = -0.4f }, StrikePoint);
                for (int i = 0; i < 50; i++)
                {
                    int dustType = i % 5 == 0 ? DustID.YellowTorch : DustID.Wraith;
                    Dust burst = Dust.NewDustPerfect(StrikePoint, dustType, new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-7f, -1f)), 60, default, 1.8f);
                    burst.noGravity = true;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float floorY = StrikePoint.Y;
            Vector2 waveCenter = new Vector2(StrikePoint.X, floorY - 16f);
            int waveType = ModContent.ProjectileType<WitchkingVoidShockwave>();
            int waveDamage = EnemyDamage.Projectile(WitchkingBoss.GrondWaveDamage);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), waveCenter, Vector2.Zero, waveType, waveDamage, 0f, Main.myPlayer, 1f, floorY);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), waveCenter, Vector2.Zero, waveType, waveDamage, 0f, Main.myPlayer, -1f, floorY);

            Vector2 mireCenter = new Vector2(StrikePoint.X, floorY - 20f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), mireCenter, Vector2.Zero,
                ModContent.ProjectileType<WitchkingBlightMire>(), 0, 0f, Main.myPlayer);
        }

        // Blight smoke thickening on the whirling head; from the lock to the slam a violet marker burns on the floor.
        private void SpawnTellDust(Vector2 head)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (Age < SlamStartTick)
            {
                int smokeCount = 1 + Age / 20;
                for (int i = 0; i < smokeCount; i++)
                {
                    int dustType = Main.rand.NextBool(5) ? DustID.YellowTorch : DustID.Wraith;
                    Dust smoke = Dust.NewDustPerfect(head + Main.rand.NextVector2Circular(10f, 10f), dustType, Vector2.Zero, 100, default, 1.3f);
                    smoke.noGravity = true;
                }
            }

            bool marking = Age >= AimLockTick && Age < ImpactTick;
            if (marking)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 mark = StrikePoint + new Vector2(Main.rand.NextFloat(-28f, 28f), -2f);
                    Dust marker = Dust.NewDustPerfect(mark, DustID.Shadowflame, new Vector2(0f, -1.2f), 40, default, 1.3f);
                    marker.noGravity = true;
                }
                Lighting.AddLight(StrikePoint, 0.4f, 0.1f, 0.55f);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            BlightBuildup.Apply(target, WitchkingMaceWhip.BlightPerHit);
        }

        // Chain: handle at the hand, 18px links along the line to the head, the head unstretched at the end.
        public override bool PreDraw(ref Color lightColor)
        {
            if (!TryGetOwner(out _, out IEnemyWhipOwner whipOwner))
            {
                return false;
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 anchor = whipOwner.WhipAnchor;
            Vector2 head = Projectile.Center;
            Vector2 line = head - anchor;
            float length = line.Length();
            float rotation = line.ToRotation() - MathHelper.PiOver2; // the sheet faces down
            Vector2 direction = line.SafeNormalize(Vector2.UnitY);

            Color handleColor = Lighting.GetColor(anchor.ToTileCoordinates());
            Main.EntitySpriteDraw(texture, anchor - Main.screenPosition, HandleFrame, handleColor, rotation,
                new Vector2(HandleFrame.Width / 2f, 16f), 1f, SpriteEffects.None, 0);

            for (float along = 18f; along < length - 12f; along += LinkFrame.Height)
            {
                Vector2 linkPosition = anchor + direction * along;
                Color linkColor = Lighting.GetColor(linkPosition.ToTileCoordinates());
                Main.EntitySpriteDraw(texture, linkPosition - Main.screenPosition, LinkFrame, linkColor, rotation,
                    new Vector2(LinkFrame.Width / 2f, 0f), 1f, SpriteEffects.None, 0);
            }

            Color headColor = Lighting.GetColor(head.ToTileCoordinates());
            Main.EntitySpriteDraw(texture, head - Main.screenPosition, HeadFrame, headColor, rotation,
                new Vector2(HeadFrame.Width / 2f, HeadFrame.Height / 2f), 1.4f, SpriteEffects.None, 0);
            return false;
        }

        private bool TryGetOwner(out NPC owner, out IEnemyWhipOwner whipOwner)
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs)
            {
                NPC candidate = Main.npc[ownerIndex];
                if (candidate.active && candidate.ModNPC is IEnemyWhipOwner anchorOwner)
                {
                    owner = candidate;
                    whipOwner = anchorOwner;
                    return true;
                }
            }

            owner = null;
            whipOwner = null;
            return false;
        }
    }
}
