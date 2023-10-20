using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace tsorcRevamp.UI
{
    public class MapMarkersUIState : UIState
    {

        public UIPanel MapMarkerUIPanel;

        public static bool Visible = false;
        public static bool Switching = false;

        public static int HoveringOver = -1;

        const int BUTTON_COUNT = 5;
        const int BUTTON_SIZE = 32;
        const int OUTER_PAD = 10;
        const int INNER_PAD = 4;

        public const int REMOVE_ID = 4;

        static readonly int WIDTH = (BUTTON_COUNT * BUTTON_SIZE) + ((BUTTON_COUNT - 1) * INNER_PAD) + OUTER_PAD * 2;
        static readonly int HEIGHT = BUTTON_SIZE + (OUTER_PAD * 2);
        public override void OnInitialize()
        {
            base.OnInitialize();
            MapMarkerUIPanel = new UIPanel();
            MapMarkerUIPanel.SetPadding(0);
            MapMarkerUIPanel.Left.Set(22, 0);
            MapMarkerUIPanel.Top.Set(22, 0);
            MapMarkerUIPanel.Width.Set(WIDTH, 0f);
            MapMarkerUIPanel.Height.Set(HEIGHT, 0f);
            MapMarkerUIPanel.BackgroundColor = new Color(35, 20, 20);

            MapMarkerButton[] buttons = new MapMarkerButton[BUTTON_COUNT];
            for (int i = 0; i < BUTTON_COUNT; i++)
            {
                Asset<Texture2D> texture = ModContent.Request<Texture2D>("tsorcRevamp/UI/Markers/" + i);
                buttons[i] = new MapMarkerButton(texture, i);
                buttons[i].Left.Set(10 + ((BUTTON_SIZE + INNER_PAD) * i), 0f);
                buttons[i].Top.Set(10, 0f);
                buttons[i].Width.Set(BUTTON_SIZE, 0f);
                buttons[i].Height.Set(BUTTON_SIZE, 0f);
            }

            foreach (MapMarkerButton button in buttons)
            {
                MapMarkerUIPanel.Append(button);
            }
            Append(MapMarkerUIPanel);
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Switching = false;
            if (!Main.mapFullscreen)
            {
                Visible = false;
                tsorcRevamp.MarkerSelected = -1;
            }
            Main.LocalPlayer.mouseInterface = true;

            if (Main.MouseScreen.X < MapMarkersUIState.WIDTH && Main.MouseScreen.Y < MapMarkersUIState.HEIGHT)
            {
                //block placing extra markers when switching markers
                Switching = true;
            }
        }
    }

    public class MapMarkerButton : UIImageButton
    {
        private int _id;
        public MapMarkerButton(Asset<Texture2D> texture, int id) : base(texture)
        {
            _id = id;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (tsorcRevamp.MarkerSelected != _id) tsorcRevamp.MarkerSelected = _id;
            else tsorcRevamp.MarkerSelected = -1;
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            MapMarkersUIState.HoveringOver = _id;
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            MapMarkersUIState.HoveringOver = -1;
        }
    }
}
