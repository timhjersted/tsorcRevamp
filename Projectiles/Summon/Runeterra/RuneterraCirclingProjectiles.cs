using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Summon.Runeterra
{
    public abstract class RuneterraCirclingProjectiles : DynamicTrail
    {
        public const float BaseSpeed = 0.03f;
        public float angularSpeed = BaseSpeed;
        public float currentAngle = 0;
        public abstract int ProjFrames { get; }
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int TrailWidth { get; }
        public abstract int TrailPointLimit { get; }
        public abstract int TrailMaxLength { get; }
        public abstract string EffectType { get; }
        public abstract string SoundPath { get; }
        public abstract int BuffType { get; }
        public abstract int dustID { get; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = ProjFrames;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.SummonTagDamageMultiplier[Projectile.type] = ScorchingPoint.BallSummonTagDmgMult / 100f;
        }
        public sealed override void SetDefaults()
        {
            Projectile.width = Width;
            Projectile.height = Height;
            Projectile.tileCollide = false;

            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minionSlots = 0.5f;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.ignoreWater = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;

            ScreenSpace = true;
            trailWidth = TrailWidth;
            trailPointLimit = TrailPointLimit;
            trailMaxLength = TrailMaxLength;
            collisionPadding = 50;
            NPCSource = false;
            trailCollision = true;
            collisionFrequency = 5;
            noFadeOut = true;
            customEffect = ModContent.Request<Effect>(EffectType, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public override bool? CanCutTiles()
        {
            return false;
        }
        public override bool MinionContactDamage()
        {
            return true;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerSummoner = player;
            int HitSound = Main.rand.Next(3);
            if (modPlayer.RuneterraMinionHitSoundCooldown < 0)
            {
                switch (HitSound)
                {
                    case 0:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "CirclingProjectileHit1") with { Volume = Projectile.ai[0] });
                            break;
                        }
                    case 1:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "CirclingProjectileHit2") with { Volume = Projectile.ai[0] });
                            break;
                        }
                    case 2:
                        {
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "CirclingProjectileHit3") with { Volume = Projectile.ai[0] });
                            break;
                        }
                }
                modPlayer.RuneterraMinionHitSoundCooldown = 20;
            }
            CustomOnHitNPC(target);
        }
        public virtual void CustomOnHitNPC(NPC target)
        {

        }
        public float trailIntensity = 1;
        public override void AI()
        {
            base.AI();

            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (angularSpeed > 0.03f)
            {
                trailIntensity = 2;
            }


            if (trailIntensity > 1)
            {
                trailIntensity -= 0.05f;
            }


            if (!CheckActive(player))
            {
                return;
            }

            if (modPlayer.InterstellarBoost)
            {
                angularSpeed = BaseSpeed * 1.5f;
            }
            if (!modPlayer.InterstellarBoost)
            {
                angularSpeed = BaseSpeed;
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    ModPacket minionPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                    minionPacket.Write(tsorcPacketID.SyncMinionRadius);
                    minionPacket.Write((byte)player.whoAmI);
                    minionPacket.Write(modPlayer.MinionCircleRadius);
                    minionPacket.Write(modPlayer.InterstellarBoost);
                    //minionPacket.WriteVector2(modPlayer.CursorPosition);
                    minionPacket.Send();
                }
            }

            currentAngle += (angularSpeed / (modPlayer.MinionCircleRadius * 0.001f + 1f));

            Vector2 offset = new Vector2(0, modPlayer.MinionCircleRadius).RotatedBy(-currentAngle);

            Projectile.Center = player.Center + offset;
            Projectile.velocity = Projectile.rotation.ToRotationVector2();
            Projectile.rotation = currentAngle * -1f;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = Vector2.Distance(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2());
            if (distance < Projectile.height * 1.2f && distance > Projectile.height * 1.2f - 32)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override float CollisionWidthFunction(float progress)
        {
            return WidthFunction(progress) - 35;
        }
        private bool CheckActive(Player owner)
        {
            if (owner.dead || !owner.active)
            {
                owner.ClearBuff(BuffType);

                return false;
            }

            if (!owner.HasBuff(BuffType))
            {
                currentAngle = 0;
                CustomCheckActive();
            }

            if (owner.HasBuff(BuffType))
            {
                Projectile.timeLeft = 2;
            }

            return true;
        }
        public virtual void CustomCheckActive()
        {
        }
        public Vector2 samplePointOffset1;
        public Vector2 samplePointOffset2;
    }
}