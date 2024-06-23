using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Core.Utils;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;

namespace tsorcRevamp.NPCs.Special
{
    class AbyssFracture : ModNPC
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

        int timer = 0;
        float expansionSpeed = 1;
        float startupPercent;
        string filterIndex = "";
        bool initialized = false;
        public override void AI()
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>()))
            {
                NPC.life = 0;
                if (Main.netMode != NetmodeID.Server && Filters.Scene[filterIndex] != null && Filters.Scene[filterIndex].IsActive())
                {
                    Filters.Scene[filterIndex].Deactivate();
                }
                NPC.active = false;
                return;
            }

            if (NPC.ai[0] == 1)
            {
                //First creation mode
                //Aura rapidly expands, becoming the abyss portal
                startupPercent += expansionSpeed / 50f;
                expansionSpeed *= 0.98f;
            }
            else
            {
                startupPercent = 1;
            }

            //Make the npc last forever
            NPC.timeLeft++;


            timer++;
            if (timer % 120 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 targetPoint = Main.npc[UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>()).Value].Center;

                for (int i = 0; i < 8; i++)
                {
                    Vector2 targetVec = UsefulFunctions.Aim(NPC.Center, targetPoint, 5.5f);
                    Vector2 spawnPos = NPC.Center + Main.rand.NextVector2Circular(50, 50);
                    float distance = Vector2.Distance(spawnPos, targetPoint) / 5.5f;
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPos, targetVec, ModContent.ProjectileType<Projectiles.Comet>(), 60, 0, Main.myPlayer, ai0: -(int)distance, ai2: 1000);
                }
            }

            //Get an unused copy of the effect from the scene filter dictionary, or create one if they're all in use
            if (!initialized && Main.netMode != NetmodeID.Server)
            {
                int index = 0;
                do
                {
                    string currentIndex = "tsorcRevamp:abyssfracture" + index;

                    //If there is an unused loaded shader, then start using it instead of creating a new one
                    if (Filters.Scene[currentIndex] != null && !Filters.Scene[currentIndex].Active)
                    {
                        Filters.Scene.Activate(currentIndex, NPC.Center).GetShader().UseTargetPosition(NPC.Center);
                        filterIndex = currentIndex;
                        tsorcRevampWorld.boundNPCShaders.Add(filterIndex, NPC);
                        initialized = true;
                        break;
                    }

                    //If we have reached the point no more entries exist, then create a new one
                    if (Filters.Scene[currentIndex] == null)
                    {
                        Filters.Scene[currentIndex] = new Filter(new ScreenShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/AbyssPortal", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "AbyssPortalPass").UseImage("Images/Misc/noise"), EffectPriority.VeryHigh);
                        filterIndex = currentIndex;
                        tsorcRevampWorld.boundNPCShaders.Add(filterIndex, NPC);
                        initialized = true;
                        break;
                    }

                    //If more than 10 are already active at once, give up and just kill the shockwave instead of creating yet another one.
                    if (index >= 20)
                    {
                        initialized = true;
                        NPC.active = false;
                        break;
                    }
                    index++;

                } while (index < 40);
            }
            if (filterIndex == "" && Main.netMode != NetmodeID.Server)
            {
                NPC.active = false;
                return;
            }

            if (Main.netMode != NetmodeID.Server && !Filters.Scene[filterIndex].IsActive())
            {
                Filters.Scene.Activate(filterIndex, NPC.Center).GetShader().UseTargetPosition(NPC.Center);
            }

            if (Main.netMode != NetmodeID.Server && Filters.Scene[filterIndex].IsActive())
            {
                float progress = startupPercent;
                Filters.Scene[filterIndex].GetShader().UseTargetPosition(NPC.Center).UseProgress(progress).UseOpacity(0.1f).UseIntensity(.3f).UseColor(new Vector3(0, 0, 0));
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return false;
        }

        public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
        {
            int bounds = (int)(50);
            boundingBox = new Rectangle((int)NPC.Center.X - bounds, (int)NPC.Center.Y - bounds, bounds * 2, bounds * 2);
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