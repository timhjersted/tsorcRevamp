using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Attraidies
{
    /// <summary>
    /// A puppet copy on the boss's 700px orbit, sharing its life. ai[0] = host, ai[1] = orbit slot, ai[2] = timer and
    /// ai[3] = the host's fight phase at spawn. Every machine derives the orbit from those; only the server fires.
    /// </summary>
    public class AttraidiesFragment : AttraidiesPuppet
    {
        private const int NormalLifetime = 900;
        private const int LaterPhaseLifetime = 1200;
        private const int FinalStandLifetime = 99999999;
        private const int FinalStandPhase = 3;
        private const int EdgeTransitionTicks = 60;
        private const float OrbitRadius = 700f;
        private const float OrbitRate = 0.006f;
        private const int ShotTellTicks = 30;
        private const int NebulaShotDamage = 45;
        private const int AuraSize = 350;

        private static readonly Color FragmentColor = new Color(170, 80, 255);
        private static Effect auraEffect;

        private int clockwise = 1;
        private float effectTimer;

        private NPC HostNPC => Main.npc[(int)NPC.ai[0]];

        private int AttackTimer
        {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        private int FightPhase => (int)NPC.ai[3];

        protected override bool ShowsTeleportFlash => false;

        // Faces the boss's synced target, so every machine agrees.
        protected override Vector2 FacingPoint => Main.player[HostNPC.target].Center;

        public override void SetDefaults()
        {
            ApplyBodyDefaults();
            NPC.npcSlots = 10;
            NPC.defense = 25;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 1500;
            NPC.timeLeft = 9999999;
            NPC.friendly = false;
            NPC.value = 3500;
        }

        public override void Unload()
        {
            auraEffect = null;
        }

        protected override void TickForm()
        {
            if (!HostNPC.active)
            {
                NPC.active = false;
                return;
            }

            int timeLimit = NormalLifetime;
            if (FightPhase != 0)
            {
                timeLimit = LaterPhaseLifetime;
            }

            if (FightPhase == FinalStandPhase)
            {
                timeLimit = FinalStandLifetime;
            }

            // Spirals out over the first 60 ticks and back in over the last 60.
            float radius = OrbitRadius;
            float angleOffset = 0f;
            if (AttackTimer < EdgeTransitionTicks)
            {
                float entryProgress = AttackTimer / (float)EdgeTransitionTicks;
                radius *= entryProgress;
                angleOffset = 1f - entryProgress;
            }

            if (AttackTimer > timeLimit - EdgeTransitionTicks)
            {
                float exitProgress = (timeLimit - AttackTimer) / (float)EdgeTransitionTicks;
                radius *= exitProgress;
                angleOffset = 1f - exitProgress;
            }

            float orbitAngle = angleOffset - 0.15f + OrbitRate * (AttackTimer + 1) + NPC.ai[1] * MathHelper.TwoPi / 5f;
            NPC.Center = HostNPC.Center + new Vector2(0, -radius).RotatedBy(orbitAngle);
            NPC.velocity = Vector2.Zero;
            NPC.realLife = HostNPC.whoAmI;
            NPC.lifeMax = HostNPC.lifeMax;
            NPC.life = HostNPC.life;

            AttackTimer++;

            int shotInterval = 0;
            if (FightPhase == FinalStandPhase)
            {
                shotInterval = 720;
            }
            else if (FightPhase == 0)
            {
                shotInterval = 120;
            }

            SetPose(AttraidiesPose.Carry);
            if (shotInterval > 0)
            {
                int cycleTick = AttackTimer % shotInterval;
                if (cycleTick >= shotInterval - ShotTellTicks)
                {
                    SetPose(AttraidiesPose.Overhead);
                    GatherDust(StaffTip, FragmentColor, 14f);
                }

                if (cycleTick == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);
                    FireNebulaShots();
                }
            }

            if (FightPhase != FinalStandPhase && AttackTimer > timeLimit && IsAuthority)
            {
                NPC.active = false;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                }
            }
        }

        private void FireNebulaShots()
        {
            if (!IsAuthority)
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 velocity = new Vector2(5, 0).RotatedBy(i * MathHelper.TwoPi / 4f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<NebulaShot>(), NebulaShotDamage, 1, Main.myPlayer, clockwise);
            }

            if (FightPhase == FinalStandPhase)
            {
                clockwise *= -1;
            }
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (!Attraidies.SharedLifeAllowsHit(projectile))
            {
                return false;
            }

            return base.CanBeHitByProjectile(projectile);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            Attraidies.RecordSharedLifeHit(projectile);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 1f, 0.4f, 0.4f);

            if (auraEffect == null || auraEffect.IsDisposed)
            {
                auraEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/AttraidiesAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Vector3 hslColor = Main.rgbToHsl(Color.Purple * 3);
            hslColor.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color auraColor = Main.hslToRgb(hslColor);

            Rectangle baseRectangle = new Rectangle(0, 0, AuraSize, AuraSize);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;

            auraEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseTurbulent.Width);
            auraEffect.Parameters["effectSize"].SetValue(baseRectangle.Size());
            auraEffect.Parameters["effectColor1"].SetValue(auraColor.ToVector4());
            auraEffect.Parameters["effectColor2"].SetValue(auraColor.ToVector4());
            auraEffect.Parameters["ringProgress"].SetValue(0);
            auraEffect.Parameters["fadePercent"].SetValue(0f);
            auraEffect.Parameters["scaleFactor"].SetValue(.5f * 50);
            auraEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.05f * 0.5f);
            auraEffect.Parameters["colorSplitAngle"].SetValue(MathHelper.TwoPi);
            auraEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, NPC.Center - Main.screenPosition, baseRectangle, Color.White, 0, baseOrigin, 1f, SpriteEffects.None, 0);
            RestoreNpcBatch();

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override bool CheckDead()
        {
            if (HostNPC.active)
            {
                NPC.life = 1;
                return false;
            }

            return true;
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit);

            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 100; i++)
            {
                int dustType = 54;
                if (i % 2 == 1)
                {
                    dustType = 62;
                }

                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, dustType, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, default, 4f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
