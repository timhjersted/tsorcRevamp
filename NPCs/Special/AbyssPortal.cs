using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp;
using Terraria.Graphics.Shaders;
using Terraria.Graphics.Effects;

namespace tsorcRevamp.NPCs.Special
{
    class AbyssPortal : ModNPC
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
            NPC.value = 600000;
            NPC.dontTakeDamage = true;
            NPC.behindTiles = true;
            NPC.ShowNameOnHover = true;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Abyssal Fissure");
        }

        bool initialized;
        float expansionSpeed = 1;
        float flashOpacity = 1;
        float startupPercent;
        string filterIndex = "tsorcRevamp:abyssportal";
        public override void AI()
        {
            if (NPC.ai[0] == 1)
            {
                //First creation mode
                //Aura rapidly expands, becoming the abyss portal
                startupPercent += expansionSpeed / 50f;
                expansionSpeed *= 0.98f;
                flashOpacity -= 0.01f;
            }
            else
            {
                flashOpacity = 0;
                startupPercent = 1;
            }

            //Make the npc last forever
            NPC.timeLeft++;

            //Make its centerpoint fixed
            NPC.Center = tsorcRevampWorld.AbyssPortalLocation;

            for(int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && Main.player[i].Center.Distance(NPC.Center) < 400 * tsorcRevampWorld.SHMScale)
                {
                    Main.player[i].AddBuff(ModContent.BuffType<Buffs.Debuffs.DarkInferno>(), 30);
                    int index = Main.player[i].FindBuffIndex(ModContent.BuffType<Buffs.Debuffs.DarkInferno>());
                    Main.player[i].buffTime[index] += 10;
                    CombatText.NewText(Main.player[i].Hitbox, Color.Red, 1);
                    Main.player[i].statLife--;
                }
            }
                        

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
                float progress = startupPercent;
                float intensity = MathHelper.Lerp(1, 1.7f, tsorcRevampWorld.SHMScale / 2f) * startupPercent;
                Filters.Scene[filterIndex].GetShader().UseTargetPosition(NPC.Center).UseProgress(progress).UseOpacity(0.1f).UseIntensity(intensity).UseColor(new Vector3(flashOpacity, 0,0));
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)        
        {
            return false;
        }

        public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
        {
            Dust.NewDust(NPC.Center, 10, 10, DustID.ShadowbeamStaff);
            int bounds = (int)(250 * tsorcRevampWorld.SHMScale);
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