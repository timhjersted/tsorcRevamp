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
using tsorcRevamp.Projectiles.Enemy.Marilith;

namespace tsorcRevamp.NPCs.Bosses.Fiends;

class MarilithIntro : ModNPC
{
    public override void SetDefaults()
    {
        NPC.scale = 1;
        NPC.npcSlots = 10;
        NPC.aiStyle = -1;
        Main.npcFrameCount[NPC.type] = 8;
        NPC.width = 40;
        NPC.height = 40;
        NPC.damage = 60;
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
    }

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("?????");
    }

    float progress = 0;
    bool tripped = false;
    public override void AI()
    {
        if(Main.GameUpdateCount % 180 == 0)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.1f, Pitch = Main.rand.NextFloat(-0.2f, 0.2f) }, NPC.Center);
        }

        NPC.aiStyle = -1;
        
        Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0.8f, 0f, 0.2f);
        if (tripped)
        {
            progress++;
        }
        
        for(int i = 0; i < Main.maxPlayers; i++)
        {
            if (Main.player[i].active && !Main.player[i].dead && NPC.Distance(Main.player[i].Center) < 400)
            {
                tripped = true;
            }
        }

        if (progress == 120)
        {
            if(Main.netMode != NetmodeID.MultiplayerClient)
            {
                Terraria.Audio.SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Thunder_0") with { Volume = 1f }, NPC.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62 with { Volume = 1.2f, Pitch = 0.9f }, NPC.Center);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CataclysmicFirestorm>(), 55, 0.5f, Main.myPlayer);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CataclysmicFirestorm>(), 55, 0.5f, Main.myPlayer);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CataclysmicFirestorm>(), 55, 0.5f, Main.myPlayer);
            }
        }
        if (progress >= 138)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)NPC.Center.X - 3, (int)NPC.Center.Y + 60 , ModContent.NPCType<FireFiendMarilith>());
            }

            NPC.active = false;
        }
    }

    
    public static ArmorShaderData data;
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)        
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        //Apply the shader, caching it as well
        if (data == null)
        {
            data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/MarilithIntro", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "MarilithIntroPass");
        }

        //data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.AcidDye), Main.LocalPlayer);

        data.UseSaturation(progress);

        //Apply the shader
        data.Apply(null);

        Rectangle recsize = new Rectangle(0, 0, tsorcRevamp.tNoiseTexture1.Width, tsorcRevamp.tNoiseTexture1.Height);

        //Draw the rendertarget with the shader
        Main.spriteBatch.Draw(tsorcRevamp.tNoiseTexture1, NPC.Center - Main.screenPosition - new Vector2(recsize.Width, recsize.Height) / 2 * 2.5f, recsize, Color.White, 0, Vector2.Zero, 2.5f, SpriteEffects.None, 0);

        //Restart the spritebatch so the shader doesn't get applied to the rest of the game
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);


        return false;
    }
    
    public override bool CheckActive()
    {
        return false;
    }
}