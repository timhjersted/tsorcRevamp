using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies
{
    ///<summary>
    ///Spellbound Ghoul: the Necromancer's raised servant (an original-TSORC enemy returning as a
    ///summon). A shambling SF4 fighter that pounces at close range, spits a slow Dream Spark from
    ///mid-range — and detonates on death: a telegraphed 300px blast of red-and-purple dreamfire,
    ///so cutting one down (or letting the Necromancer sacrifice it) still demands respect.
    ///Sheet layout: frame 0 idle, 1 jump, 2-7 walk. Stats step up in Hardmode on the first AI tick.
    ///</summary>
    public class SpellboundGhoul : ModNPC
    {
        const int SparkTelegraphTicks = 20;
        const int SparkCooldownTicks = 240;

        bool statsInitialized;
        int sparkCooldown = 120;
        int sparkTimer; //>0 = mid-cast

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 18;
            NPC.height = 40;
            NPC.damage = 30;
            NPC.defense = 10;
            NPC.lifeMax = 150;
            NPC.HitSound = SoundID.NPCHit2;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 0f; //a summon, not a soul farm
            NPC.npcSlots = 0f;
            NPC.knockBackResist = 0.5f;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().NavSearchRadius = 40; //modest A* so it can path to raised positions
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) { return 0f; }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((short)sparkCooldown);
            writer.Write((byte)sparkTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            sparkCooldown = reader.ReadInt16();
            sparkTimer = reader.ReadByte();
        }

        public override void AI()
        {
            if (!statsInitialized)
            {
                statsInitialized = true;
                if (Main.hardMode)
                {
                    NPC.lifeMax = 300;
                    NPC.life = NPC.lifeMax;
                    NPC.damage = 55;
                    NPC.defense = 20;
                    NPC.netUpdate = true;
                }
            }
            Player player = Main.player[NPC.target];

            //Dream Spark: a slow drifting wisp bolt from mid-range, so it isn't pure melee
            if (sparkTimer > 0)
            {
                RunDreamSpark(player);
                return;
            }

            tsorcRevampAIs.FighterAI(NPC, topSpeed: 1.8f, acceleration: 0.08f, canPounce: true);

            if (sparkCooldown > 0)
            {
                sparkCooldown--;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && sparkCooldown <= 0 && NPC.velocity.Y == 0f
                && !player.dead && player.active)
            {
                float distTiles = NPC.Distance(player.Center) / 16f;
                if (distTiles > 6f && distTiles < 20f && Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1))
                {
                    sparkTimer = 1;
                    NPC.netUpdate = true;
                }
            }

            //Dream-mist drifting off it
            if (Main.rand.NextBool(6))
            {
                Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                int mist = Dust.NewDust(pos, 4, 4, DustID.Shadowflame, 0f, -0.4f, 150, default, 0.9f);
                Main.dust[mist].noGravity = true;
                Main.dust[mist].velocity *= 0.25f;
            }
            Lighting.AddLight(NPC.Center, 0.12f, 0.06f, 0.2f);
        }

        void RunDreamSpark(Player player)
        {
            NPC.velocity.X *= 0.8f;
            NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            NPC.spriteDirection = NPC.direction;
            sparkTimer++;

            Vector2 mouth = NPC.Center + new Vector2(NPC.direction * 12f, -8f);
            //Purple glint converging at the mouth
            if (sparkTimer <= SparkTelegraphTicks)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = mouth + angle.ToRotationVector2() * Main.rand.NextFloat(6f, 20f);
                int dust = Dust.NewDust(pos, 4, 4, DustID.Shadowflame, 0f, 0f, 90, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = (mouth - pos) * 0.15f;
            }
            if (sparkTimer == SparkTelegraphTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = (player.Center - mouth).SafeNormalize(new Vector2(NPC.direction, 0f)) * 6.5f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                    ModContent.ProjectileType<GhoulDreamSpark>(), Main.hardMode ? 22 : 12, 1f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.4f, Pitch = 0.4f }, NPC.Center);
            }
            if (sparkTimer >= SparkTelegraphTicks + 14)
            {
                sparkTimer = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    sparkCooldown = SparkCooldownTicks + Main.rand.Next(60);
                    NPC.netUpdate = true;
                }
            }
        }

        ///<summary>Sheet layout: frame 0 idle, 1 jump, 2-7 walk.</summary>
        public override void FindFrame(int frameHeight)
        {
            if (NPC.velocity.Y != 0f)
            {
                NPC.frame.Y = 1 * frameHeight; //jump
                NPC.frameCounter = 0;
                return;
            }
            if (System.Math.Abs(NPC.velocity.X) > 0.1f)
            {
                NPC.frameCounter += System.Math.Abs(NPC.velocity.X) * 0.5f + 0.5f;
                if (NPC.frameCounter >= 8.0)
                {
                    NPC.frameCounter = 0;
                    NPC.frame.Y += frameHeight;
                }
                if (NPC.frame.Y < 2 * frameHeight || NPC.frame.Y > 7 * frameHeight)
                {
                    NPC.frame.Y = 2 * frameHeight; //walk cycle: frames 2-7
                }
            }
            else
            {
                NPC.frame.Y = 0; //idle
                NPC.frameCounter = 0;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 3; i++)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, hit.HitDirection, -1f, 100, default, 1f);
                Main.dust[dust].noGravity = true;
            }
            if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
            {
                //It was never fully here: dissolves into dream-stuff instead of gore
                for (int i = 0; i < 20; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, vel.X, vel.Y - 1f, 80, default, 1.4f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }

        public override void OnKill()
        {
            //The binding snaps: a telegraphed dreamfire detonation where it fell
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<GhoulDeathBlast>(), Main.hardMode ? 26 : 14, 3f, Main.myPlayer, 40f);
            }
        }
    }
}
