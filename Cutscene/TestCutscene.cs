//if we include everything, we can't possibly forget something later!
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using Terraria.UI;
using Terraria.GameContent.UI;
using tsorcRevamp.UI;
using Microsoft.Xna.Framework.Graphics;
using static tsorcRevamp.MethodSwaps;
using static tsorcRevamp.ILEdits;
using System.IO;
using Terraria.ModLoader.IO;
using Terraria.Graphics.Shaders;
using Terraria.Graphics.Effects;
using ReLogic.Graphics;
using System.Net;
using System.Reflection;
using System.ComponentModel;
using Terraria.GameContent.UI.Elements;

namespace tsorcRevamp {
    class TestCutscene : Cutscene {
        private Player player;
        //private NPC FearsomeBlueSlime;
        public override string name => "TestCutscene";

        public TestCutscene(Player player) {
            this.player = player;
        }

        public override void Start() {
            base.Start();
            if (Main.netMode != NetmodeID.SinglePlayer) {
                ModPacket cutscenePacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                cutscenePacket.Write(tsorcPacketID.CutsceneStart);
                cutscenePacket.Write((byte)CutsceneType.TestCutscene);
                cutscenePacket.Write(false);
                cutscenePacket.Write((byte)player.whoAmI);
                cutscenePacket.Send();
            }
        }

        public override void UpdateCutscene() {
            timer++;
            if (timer == 1) {
                Main.PlaySound(SoundID.Item6);
                tsorcRevamp.instance.GlobalUI?.SetState(new TestCutsceneUI(timer));
            }
            if (timer == 60) {
                player.velocity.Y -= 35;
                Interrupt(InterruptType.Stop);
            }
            /*
             if (timer == 120) {
                //Creating a new NPC and using it in the cutscene could be done like this:
                FearsomeBlueSlime = Main.npc[NPC.NewNPC(player.position.X, player.position.Y + 50, NPCID.BlueSlime)];
                //referencing an existing one could be like this
                FearsomeBlueSlime = Main.npc[NPC.FindFirstNPC(NPCID.BlueSlime)];
            }
             */

        }

        public override float CameraZoom() {
            return ((float)timer / (float)30);
        }

        public override Vector2 CameraPosition() {
            return new Vector2(0, -2f * timer).RotatedBy(0.1f * timer);
        }

        public override void Draw(SpriteBatch spriteBatch) {
            string memeText = "text up here";
            Vector2 memeTextOrigin = Main.fontDeathText.MeasureString(memeText);
            Vector2 memeTextPosition = new Vector2((Main.screenWidth / 2) - memeTextOrigin.X * 0.5f, 70);
            DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, Main.fontDeathText, memeText, memeTextPosition, Main.DiscoColor, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

        }

        public override void Interrupt(InterruptType interruptType) {
            if (interruptType == InterruptType.Stop) {
                base.Interrupt(interruptType); 
            }
            else {
                paused = true;
            }
        }
    }
    class TestCutsceneUI : UIState {
        private int timer;
        //mostly redundant on account of Cutscene.Draw, but it may end up being useful to separate UI from the cutscene
        public override void Draw(SpriteBatch spriteBatch) {
            
            string memeText = "and text down here";
            Vector2 memeTextOrigin = Main.fontDeathText.MeasureString(memeText);
            Vector2 memeTextPosition = new Vector2((Main.screenWidth / 2) - memeTextOrigin.X * 0.5f, 70 + (80 * 6));
            DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, Main.fontDeathText, memeText, memeTextPosition, Main.DiscoColor, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            
        }

        public override void Update(GameTime gameTime) {
            timer++;
            
            if (tsorcRevamp.instance.cutscene == null || timer > 60) {
                tsorcRevamp.instance.HideCutsceneUI();
                return;
            }
        }

        public TestCutsceneUI(int timer) {
            this.timer = timer;
        }
    }
}
