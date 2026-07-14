using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>Silent intermission between Khaios and his spirit form.</summary>
    public class KhaiosTransitionOrb : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Invaders/InvaderPlaceholder";

        private const int PauseTicks = 180;
        private const float RiseDistance = 8f * 16f;
        private static ArmorShaderData orbShader;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.aiStyle = -1;
            NPC.lifeMax = 1;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.alpha = 80;
            NPC.npcSlots = 0f;
        }

        public override void AI()
        {
            NPC.ai[0]++;
            Lighting.AddLight(NPC.Center, new Vector3(1f, 0.78f, 0.28f));

            if (Main.netMode != NetmodeID.MultiplayerClient && !AnyLivingPlayerNearby())
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            if (NPC.ai[0] < PauseTicks)
            {
                NPC.velocity = Vector2.Zero;
                return;
            }

            NPC.velocity = new Vector2(0f, -1f);
            NPC.ai[1] += 1f;
            if (NPC.ai[1] < RiseDistance)
            {
                return;
            }

            NPC.velocity = Vector2.Zero;
            CreateTransformationBurst();
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y,
                    ModContent.NPCType<SpiritOfKhaios>());
                NPC.active = false;
                NPC.netUpdate = true;
            }
        }

        private bool AnyLivingPlayerNearby()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.DistanceSQ(NPC.Center) < 3000f * 3000f)
                {
                    return true;
                }
            }
            return false;
        }

        private void CreateTransformationBurst()
        {
            if (NPC.localAI[0] != 0f || Main.dedServ)
            {
                return;
            }
            NPC.localAI[0] = 1f;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1.1f, Pitch = 0.15f }, NPC.Center);
            for (int i = 0; i < 90; i++)
            {
                int dustType = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Cloud;
                Dust dust = Dust.NewDustPerfect(NPC.Center, dustType,
                    Main.rand.NextVector2Circular(8f, 8f), 0, Color.White, Main.rand.NextFloat(1.3f, 2.3f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            orbShader ??= new ArmorShaderData(
                new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/MarilithIntro", AssetRequestMode.ImmediateLoad).Value),
                "MarilithIntroPass");
            float pulse = 35f + 18f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            orbShader.UseSaturation(pulse);
            orbShader.Apply(null);

            Rectangle source = tsorcRevamp.NoiseTurbulent.Bounds;
            float scale = 1.25f + 0.08f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Main.spriteBatch.Draw(tsorcRevamp.NoiseTurbulent, NPC.Center - Main.screenPosition,
                source, Color.White, 0f, source.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        public override bool CheckActive() => false;
    }
}
