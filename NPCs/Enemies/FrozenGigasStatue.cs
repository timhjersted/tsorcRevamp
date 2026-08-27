using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies
{
    ///<summary>
    ///The Ice Gigas's Frozen Echo: a brittle, translucent ice statue of the giant, left behind when it
    ///blinks away (and summoned in pairs by Mirror of Winter). Reuses the IceGigas sheet drawn in a
    ///pale translucent blue — no bespoke sprite. Harmless to touch, but it SHATTERS into a radial
    ///spray of ice shards when killed or when its timer runs out — the first time, the player is
    ///beating on a bomb without knowing.
    ///ai[0] = lifetime in ticks (default 300). ai[1] = 1: "armed" (Mirror of Winter) — fires a
    ///five-icicle fan after ~1s of existence, then shatters on its own shortly after.
    ///</summary>
    public class FrozenGigasStatue : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Enemies/IceGigas";

        const int ArmedFanTick = 60;
        const int ArmedShatterTick = 90;

        //Mid-Hardmode values; SHM encounters escalate like the parent (×1.3 × SHMScale)
        int FanDamage => ScaleDamage(36);
        int ShatterShardDamage => ScaleDamage(28);
        static int ScaleDamage(int baseDamage) => tsorcRevampWorld.SuperHardMode ? (int)(baseDamage * 1.3f * tsorcRevampWorld.SHMScale) : baseDamage;

        int Lifetime => NPC.ai[0] > 0f ? (int)NPC.ai[0] : 300;
        bool Armed => NPC.ai[1] == 1f;
        int Timer { get => (int)NPC.ai[2]; set => NPC.ai[2] = value; }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16; //shares the IceGigas sheet; we always draw frame 0 (idle)
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 52;
            NPC.height = 110;
            NPC.damage = 0; //harmless to touch — the danger is the shatter
            NPC.defense = 18;
            NPC.lifeMax = 250; //~2-4 mid-Hardmode hits: brittle, but not free
            NPC.HitSound = SoundID.Item27;
            NPC.DeathSound = SoundID.Item27 with { Pitch = -0.5f };
            NPC.value = 0f;
            NPC.npcSlots = 0f;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) { return 0f; }

        public override void AI()
        {
            NPC.velocity.X = 0f; //rooted; gravity may settle it onto the ground
            Timer++;

            //Cold mist drifting off the ice
            if (Main.rand.NextBool(3))
            {
                Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, -0.5f, 140, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }
            //Aging read: cracking glints get denser as the timer runs down
            float age = Timer / (float)Lifetime;
            if (Main.rand.NextFloat() < 0.1f + age * 0.25f)
            {
                int glint = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceRod, 0f, 0f, 100, default, 0.8f);
                Main.dust[glint].noGravity = true;
                Main.dust[glint].velocity *= 0.2f;
            }
            Lighting.AddLight(NPC.Center, 0.2f, 0.35f, 0.55f);

            //Armed (Mirror of Winter): fire the icicle fan, then self-shatter
            if (Armed && Timer == ArmedFanTick && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 pos = NPC.Center + new Vector2((i - 2) * 22f, -70f - System.Math.Abs(i - 2) * -8f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, Vector2.Zero,
                        ModContent.ProjectileType<GigasCrownIcicle>(), FanDamage, 2f, Main.myPlayer, 15f + i * 5f);
                }
                SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.7f, Pitch = -0.2f }, NPC.Center);
            }

            int endTick = Armed ? ArmedShatterTick : Lifetime;
            if (Timer >= endTick && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.life = 0;
                NPC.HitEffect();
                NPC.checkDead();
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            //Crystal chips on every hit
            for (int i = 0; i < 4; i++)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Ice, hit.HitDirection, -1f, 60, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }
            if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
            {
                //~80 ice dust total for the shatter — this is the trap springing, so it should
                //read as a genuine explosion of ice rather than a light crumble.
                for (int i = 0; i < 60; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Ice, vel.X, vel.Y, 40, default, Main.rand.NextFloat(1.2f, 1.8f));
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 20; i++)
                {
                    int glint = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceRod, 0f, -2f, 60, default, 1.1f);
                    Main.dust[glint].noGravity = true;
                }
            }
        }

        public override void OnKill()
        {
            //The trap springs: radial shard burst
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 vel = (MathHelper.TwoPi * i / 8f + Main.rand.NextFloat(0.3f)).ToRotationVector2() * Main.rand.NextFloat(5f, 7f);
                    Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, vel,
                        ModContent.ProjectileType<GigasIceShard>(), ShatterShardDamage, 1f, Main.myPlayer, 0.1f);
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (Main.dedServ)
            {
                return; //textures aren't loaded on a dedicated server
            }
            NPC.frame = new Rectangle(0, 0, TextureAssets.Npc[NPC.type].Value.Width, frameHeight); //always the idle frame
        }

        ///<summary>Draw the IceGigas sheet as translucent pale-blue ice — statue, not creature.</summary>
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            //Same ground-planting shift the living giant gets from DrawOffsetY. Shared constant
            //rather than a matching literal, so the two copies of this sprite cannot drift apart.
            Vector2 drawPos = NPC.position + new Vector2(NPC.width / 2f, NPC.height + NPC.gfxOffY + IceGigas.SpriteDrawOffsetY) - screenPos;
            Vector2 origin = new Vector2(NPC.frame.Width / 2f, NPC.frame.Height);
            Color tint = new Color(150, 215, 255, 140);
            //Faint white "ice" backing pass, slightly larger, then the tinted body
            spriteBatch.Draw(texture, drawPos, NPC.frame, new Color(200, 240, 255, 40), NPC.rotation, origin, NPC.scale * 1.04f, effects, 0f);
            spriteBatch.Draw(texture, drawPos, NPC.frame, tint, NPC.rotation, origin, NPC.scale, effects, 0f);
            return false;
        }
    }
}
