using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class Conqueror : ModBuff
    {
        public const int FrameCount = 10;
        private Asset<Texture2D> animatedTexture;
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            if (Main.netMode != NetmodeID.Server)
            {
                // Do NOT load textures on the server!
                animatedTexture = ModContent.Request<Texture2D>(Texture + "Sheet");
            }
        }
        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.BotCConquerorStacks == 0)
            {
                modPlayer.BotCConquerorStacks = 1;
            }
            if (player.buffTime[buffIndex] == 1)
            {
                if (modPlayer.BotCConquerorStacks > 1)
                {
                    modPlayer.BotCConquerorStacks--;
                    player.buffTime[buffIndex] = (int)(((float)modPlayer.BotCConquerorDuration / 6f) * 60f);
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFallOff") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.2f }, player.Center);
                }
                else
                {
                    modPlayer.BotCConquerorStacks = 0;
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFallOff") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.4f }, player.Center);
                }
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
        {
            // You can use this hook to make something special happen when the buff icon is drawn (such as reposition it, pick a different texture, etc.).

            // We draw our special texture here with a specific animation.

            // Use our animation spritesheet.
            Texture2D ourTexture = animatedTexture.Value;
            // Choose the frame to display, here based on constants and the game's tick count.
            Rectangle ourSourceRectangle = animatedTexture.Frame(verticalFrames: FrameCount, frameY: (int)Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BotCConquerorStacks - 1);

            // Other stuff you can do in this hook
            /*
			// Here we make the icon have a lime green tint.
			drawParams.drawColor = Color.LimeGreen * Main.buffAlpha[buffIndex];
			*/

            // Be aware of the fact that drawParams.mouseRectangle exists: it defaults to the size of the autoloaded buffs' sprite,
            // it handles mouseovering and clicking on the buff icon. Since our frame in the animation is 32x32 (same as the autoloaded sprite),
            // and we don't change drawParams.position, we don't have to do anything. If you offset the position, or have a non-standard size, change it accordingly.

            // We have two options here:
            // Option 1 is the recommended one, as it requires less code.
            // Option 2 allows you to customize drawing even more, but then you are on your own.

            // For demonstration, both options' codes are written down, but the latter is commented out using /* and */.

            // OPTION 1 - Let the game draw it for us. Therefore we have to assign our variables to drawParams:
            drawParams.Texture = ourTexture;
            drawParams.SourceRectangle = ourSourceRectangle;
            // Return true to let the game draw the buff icon.
            return true;

            /*
			// OPTION 2 - Draw our buff manually:
			spriteBatch.Draw(ourTexture, drawParams.position, ourSourceRectangle, drawParams.drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

			// Return false to prevent drawing the icon, since we have already drawn it.
			return false;
			*/
        }


        public override bool ReApply(Player player, int time, int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.BotCConquerorStacks < modPlayer.BotCConquerorMaxStacks)
            {
                modPlayer.BotCConquerorStacks++;
            }

            return false;
        }
    }
}
