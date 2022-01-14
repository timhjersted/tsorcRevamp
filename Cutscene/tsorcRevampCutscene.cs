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

namespace tsorcRevamp {
    public abstract class Cutscene {

        public int timer;
        public bool paused;

        /// <summary>
        /// unused, but may be useful later
        /// </summary>
        public abstract string name {
            get;
        }

        /// <summary>
        /// The cutscene's main update loop
        /// </summary>
        public abstract void UpdateCutscene();

        /// <summary>
        /// Suitable for update tasks that require a spritebatch, such as UI.
        /// </summary>
        /// <param name="spriteBatch">A spritebatch</param>
        public virtual void Draw(SpriteBatch spriteBatch) {
        }

        /// <summary>
        /// Change the camera position
        /// </summary>
        /// <returns></returns>
        public virtual Vector2 CameraPosition() {
            return Vector2.Zero;
        }

        /// <summary>
        /// Allows changing of camera zoom during a cutscene.
        /// </summary>
        /// <returns>A value additive to the default zoom of 1f. For instance, to double the zoom, return 1f. Negative values not recommended.</returns>
        public virtual float CameraZoom() {
            return 0f;
        }

        /// <summary>
        /// Sync the cutscene here. Changing anything else on cutscene start (time of day, weather) also goes here
        /// </summary>
        public virtual void Start() {
            tsorcRevamp.instance.cutscene = this;
        }

        /// <summary>
        /// Stop or pause the cutscene
        /// </summary>
        /// <param name="interruptType">Stop or pause</param>
        public virtual void Interrupt(InterruptType interruptType = 0) {
            tsorcRevamp.instance.cutscene = null;
        }

        public enum InterruptType {
            Stop = 0, 
            Pause = 1
        }

        public enum CutsceneType {
            TestCutscene = 0
        }
    }
}
