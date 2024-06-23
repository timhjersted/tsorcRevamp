using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Special
{
    class AbyssCataclysm : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.scale = 1;
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            Main.npcFrameCount[NPC.type] = 8;
            NPC.width = 40;
            NPC.height = 40;
            NPC.defense = 38;
            AnimationType = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 300000;
            NPC.timeLeft = 22500;
            NPC.alpha = 100;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.value = 0;
            NPC.dontTakeDamage = true;
            NPC.behindTiles = true;
            NPC.ShowNameOnHover = true;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Abyssal Fissure");
        }

        float expansionSpeed = .6f;
        float flashOpacity = 0.3f;
        float startupPercent;
        string filterIndex = "tsorcRevamp:abysscataclysm";
        float timer = 780;
        public override void AI()
        {
            timer--;
            if(timer == 0)
            {
                NPC.life = 0;
                if (Main.netMode != NetmodeID.Server && Filters.Scene[filterIndex] != null && Filters.Scene[filterIndex].IsActive())
                {
                    Filters.Scene[filterIndex].Deactivate();
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.StrikeInstantKill();
                    return;
                }
            }

            //Instantly kill all players regardless of distance
            for(int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    Main.player[i].immune = false;
                    Main.player[i].statLife -= 999999999;
                    Main.player[i].KillMe(PlayerDeathReason.ByNPC(NPC.whoAmI), 999999999, 0);
                }
            }

            startupPercent += expansionSpeed / 50f;
            expansionSpeed *= 1.01f;
            flashOpacity -= 0.01f;
            if (flashOpacity < 0)
            {
                flashOpacity = 0;
            }

            //Make the npc last forever
            NPC.timeLeft++;


            if (Filters.Scene[filterIndex] == null)
            {
                Filters.Scene[filterIndex] = new Filter(new ScreenShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/AbyssPortal", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "AbyssPortalPass").UseImage("Images/Misc/noise"), EffectPriority.VeryHigh);
            }

            if (Main.netMode != NetmodeID.Server && !Filters.Scene[filterIndex].IsActive())
            {
                Filters.Scene.Activate(filterIndex, NPC.Center).GetShader().UseTargetPosition(NPC.Center);
            }

            if (Main.netMode != NetmodeID.Server && Filters.Scene[filterIndex].IsActive())
            {
                float progress = startupPercent; //Distortion
                float intensity = startupPercent; //Size
                if(timer < 60)
                {
                    progress *= (timer / 60f);
                }
                Filters.Scene[filterIndex].GetShader().UseTargetPosition(NPC.Center).UseProgress(progress).UseOpacity(0.1f).UseIntensity(intensity).UseColor(new Vector3(flashOpacity, 0, 0));
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return false;
        }

        public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
        {
            boundingBox = new Rectangle(0, 0, 99999, 99999);
        }

        public override void OnKill()
        {
            if (Filters.Scene[filterIndex] != null && Filters.Scene[filterIndex].IsActive())
            {
                Filters.Scene[filterIndex].Deactivate();
            }
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}