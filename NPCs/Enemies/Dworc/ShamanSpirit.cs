using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Dworc;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.Dworc
{
    public class ShamanSpirit : ModNPC
    {
        public const int ShamanExpertLife = 200;
        public const int AbyssExpertLife = 800;
        public const int ArrivalTicks = 24;
        public const int PursuitTicks = 320;
        public const int FuseTicks = 24;
        public const float BlastRadius = 92f;
        private int ownerIndex = -1;
        private int ownerToken;
        private bool abyss;
        private bool initialized;
        private bool finishingDeath;
        private byte state;
        private int age;
        private int targetIndex;
        private int arcSide;
        private Vector2 fuseCenter;

        private bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
        public bool BelongsTo(int index, int token) => ownerIndex == index && ownerToken == token;
        public bool CanReceiveHealing => state < 2;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 6;
            NPCID.Sets.NPCBestiaryDrawModifiers hidden = new NPCID.Sets.NPCBestiaryDrawModifiers();
            hidden.Hide = true;
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, hidden);
        }

        public override void SetDefaults()
        {
            NPC.width = 36;
            NPC.height = 30;
            NPC.lifeMax = 100;
            NPC.defense = 0;
            NPC.damage = 0;
            NPC.value = 0;
            NPC.npcSlots = 0f;
            NPC.dontCountMe = true;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lavaImmune = true;
            NPC.netAlways = true;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit3;
            NPC.alpha = 255;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().SuppressGlobalOnKillDrops = true;
        }

        public void Initialize(int index, int token, bool abyssVariant)
        {
            ownerIndex = index;
            ownerToken = token;
            abyss = abyssVariant;
            initialized = true;
            arcSide = NPC.whoAmI % 3 - 1;
            int expertLife = abyss ? AbyssExpertLife : ShamanExpertLife;
            float ratio = Main.GameModeInfo.EnemyMaxLifeMultiplier / 2f;
            NPC.lifeMax = Math.Max(1, (int)MathF.Round(expertLife * ratio));
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;
        }

        public override bool CheckActive() => false;

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override void AI()
        {
            if (!initialized)
            {
                if (Authority)
                {
                    NPC.active = false;
                    NPC.netUpdate = true;
                }

                return;
            }

            bool ownerValid = ownerIndex >= 0 && ownerIndex < Main.maxNPCs
                && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].ModNPC is DworcCasterPuppet caster
                && caster.SpawnToken == ownerToken;
            if (Authority && state != 3)
            {
                NPC.TargetClosest(false);
                if (!ownerValid || !NPC.HasValidTarget)
                {
                    state = 3;
                    age = 0;
                    NPC.velocity = Vector2.Zero;
                    NPC.dontTakeDamage = true;
                    NPC.netUpdate = true;
                }
                else if (state == 1 && targetIndex != NPC.target)
                {
                    targetIndex = NPC.target;
                    NPC.netUpdate = true;
                }
            }

            age++;
            if (state == 0)
            {
                NPC.alpha = (int)MathHelper.Lerp(255f, 40f, MathHelper.Clamp(age / (float)ArrivalTicks, 0f, 1f));
                NPC.velocity = Vector2.Zero;
                if (Authority && age >= ArrivalTicks)
                {
                    state = 1;
                    age = 0;
                    targetIndex = NPC.target;
                    NPC.netUpdate = true;
                }
            }
            else if (state == 1)
            {
                NPC.alpha = 40;
                if (targetIndex >= 0 && targetIndex < Main.maxPlayers && Main.player[targetIndex].active && !Main.player[targetIndex].dead)
                {
                    Player target = Main.player[targetIndex];
                    if (age < PursuitTicks - 60)
                    {
                        Vector2 point = target.Center;
                        if (age < PursuitTicks / 2)
                        {
                            point.Y += arcSide * 160f;
                        }

                        NPC.velocity = (point - NPC.Center).SafeNormalize(Vector2.UnitY) * 4.5f;
                    }

                    if (Authority && (NPC.Distance(target.Center) <= 40f || age >= PursuitTicks))
                    {
                        StartFuse();
                    }
                }
            }
            else if (state == 2)
            {
                NPC.life = 1;
                NPC.Center = fuseCenter;
                NPC.velocity = Vector2.Zero;
                NPC.alpha = 15;
                if (Authority && age >= FuseTicks)
                {
                    int expertDamage = abyss ? DworcCasterPuppet.AbyssSpiritExpertDamage : DworcCasterPuppet.ShamanSpiritExpertDamage;
                    Projectile.NewProjectile(NPC.GetSource_Death(), fuseCenter, Vector2.Zero,
                        ModContent.ProjectileType<DworcSpiritExplosion>(), EnemyDamage.Projectile(expertDamage), 0f, Main.myPlayer);
                    finishingDeath = true;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        // Publish pending-death completion before the instant-kill hit packet, so
                        // a victim client does not intercept that authoritative kill as a new fuse.
                        NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                    }

                    NPC.StrikeInstantKill();
                }
            }
            else
            {
                NPC.alpha = (int)MathHelper.Lerp(40f, 255f, MathHelper.Clamp(age / 12f, 0f, 1f));
                if (Authority && age >= 12)
                {
                    NPC.active = false;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                    }
                }
            }

            if (Math.Abs(NPC.velocity.X) > 0.1f)
            {
                NPC.spriteDirection = NPC.velocity.X > 0f ? 1 : -1;
            }

            NPC.rotation = MathHelper.Clamp(NPC.velocity.X * 0.025f, -0.12f, 0.12f);
            if (!Main.dedServ)
            {
                Lighting.AddLight(NPC.Center, 0.2f, 0.08f, 0.35f);
                if (state == 2)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(BlastRadius, BlastRadius);
                        Dust warning = Dust.NewDustPerfect(NPC.Center + offset, DustID.ShadowbeamStaff, -offset * 0.025f, 130, Color.MediumPurple, 0.7f);
                        warning.noGravity = true;
                    }
                }
                else if (age % 3 == 0)
                {
                    Dust trail = Dust.NewDustPerfect(NPC.Center, DustID.ShadowbeamStaff, -NPC.velocity * 0.15f, 140, Color.MediumPurple, 0.75f);
                    trail.noGravity = true;
                }
            }
        }

        private void StartFuse()
        {
            state = 2;
            age = 0;
            fuseCenter = NPC.Center;
            NPC.velocity = Vector2.Zero;
            NPC.life = Math.Max(1, NPC.life);
            NPC.dontTakeDamage = true;
            NPC.chaseable = false;
            NPC.netUpdate = true;
        }

        public override bool CheckDead()
        {
            if (finishingDeath)
            {
                return true;
            }

            NPC.life = 1;
            if (Authority && state < 2)
            {
                StartFuse();
            }

            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            if (NPC.frameCounter >= 6)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * 6);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                Dust mote = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(12f, 12f), DustID.ShadowbeamStaff,
                    Main.rand.NextVector2Circular(2f, 2f), 100, Color.MediumPurple, 0.8f);
                mote.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[Type].Value;
            SpriteEffects effects = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
            {
                effects = SpriteEffects.FlipHorizontally;
            }

            spriteBatch.Draw(texture, NPC.Center - screenPos, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation,
                new Vector2(26f, 21f), NPC.scale, effects, 0f);
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(ownerIndex);
            writer.Write(ownerToken);
            writer.Write(abyss);
            writer.Write(initialized);
            writer.Write(finishingDeath);
            writer.Write(state);
            writer.Write(age);
            writer.Write(targetIndex);
            writer.Write(arcSide);
            writer.WriteVector2(fuseCenter);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ownerIndex = reader.ReadInt32();
            ownerToken = reader.ReadInt32();
            abyss = reader.ReadBoolean();
            initialized = reader.ReadBoolean();
            finishingDeath = reader.ReadBoolean();
            state = reader.ReadByte();
            age = reader.ReadInt32();
            targetIndex = reader.ReadInt32();
            arcSide = reader.ReadInt32();
            fuseCenter = reader.ReadVector2();
            int expertLife = abyss ? AbyssExpertLife : ShamanExpertLife;
            NPC.lifeMax = Math.Max(1, (int)MathF.Round(expertLife * Main.GameModeInfo.EnemyMaxLifeMultiplier / 2f));
            NPC.dontTakeDamage = state >= 2;
            NPC.chaseable = state < 2;
        }
    }
}
