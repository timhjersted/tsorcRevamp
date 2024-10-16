using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Windows.Forms;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.UI
{
    class CustomMapUIState : UIState
    {

        public static bool Visible = false;

        bool invalidMap = false;

        tsorcUICenteredTextButton newCustomMap;
        public override void OnInitialize()
        {
            ConstructUI();
        }

        public void ConstructUI()
        {
            RemoveAllChildren();
            UIElement mapSelector = new UIElement
            {
                Width = StyleDimension.FromPixels(800f),
                Height = StyleDimension.FromPixels(950f),
                Top = StyleDimension.FromPixels(170f),
                HAlign = 0.5f,
                VAlign = 0f,
            };

            tsorcUICenteredTextButton selectorBackground = new tsorcUICenteredTextButton(LangUtils.GetTextValue("UI.SelectWorld"), .85f, true)
            {
                Width = StyleDimension.FromPercent(1f),
                Height = StyleDimension.FromPixels(330),
                Top = StyleDimension.FromPixels(100f),
                TextVAlign = 0.075f,
                BackgroundColor = new Color(33, 43, 79) * 0.8f
            };


            newCustomMap = new tsorcUICenteredTextButton(LangUtils.GetTextValue("UI.TSORCMap"), .85f, true)
            {
                Width = StyleDimension.FromPercent(1f),
                Height = StyleDimension.FromPixels(100),
                Top = StyleDimension.FromPixels(80f),
                HAlign = 0.5f,
                BackgroundColor = new Color(33, 43, 120).MultiplyRGB(Main.DiscoColor * 0.9f)
            };
            newCustomMap.OnMouseOver += HoverCustomMap;
            newCustomMap.OnMouseOut += UnselectCustomMap;
            newCustomMap.OnLeftMouseDown += CustomMapSelected;


            tsorcUICenteredTextButton newVanillaMap = new tsorcUICenteredTextButton(LangUtils.GetTextValue("UI.VanillaMap"), .85f, true)
            {
                Width = StyleDimension.FromPercent(1f),
                Height = StyleDimension.FromPixels(100),
                Top = StyleDimension.FromPixels(200f),
                HAlign = 0.5f,
                TextVAlign = 0.5f,
                BackgroundColor = new Color(33, 43, 120)
            };
            newVanillaMap.OnMouseOver += HoverVanillaMap;
            newVanillaMap.OnMouseOut += UnselectVanillaMap;
            newVanillaMap.OnLeftMouseDown += VanillaMapSelected;

            selectorBackground.Append(newCustomMap);
            selectorBackground.Append(newVanillaMap);

            tsorcUICenteredTextButton backButton = new tsorcUICenteredTextButton(LangUtils.GetTextValue("UI.Back"), .85f, true)
            {
                Width = StyleDimension.FromPercent(1f),
                Height = StyleDimension.FromPixels(80),
                Top = StyleDimension.FromPixels(450f),
                BackgroundColor = new Color(33, 43, 79),
                TextHAlign = 0.5f,

            };

            backButton.OnMouseOver += HoverVanillaMap;
            backButton.OnMouseOut += UnselectBackButton;
            backButton.OnLeftMouseDown += BackButtonPressed;

            mapSelector.Append(backButton);

            mapSelector.Append(selectorBackground);


            Append(mapSelector);
        }

        private void NewCustomMap_OnUpdate(UIElement affectedElement)
        {
            UITextPanel<string> targetPanel = affectedElement as UITextPanel<string>;
        }

        public override void Update(GameTime gameTime)
        {
            newCustomMap.BackgroundColor = new Color(33, 43, 120).MultiplyRGB(Main.DiscoColor * 0.9f);
            if (newCustomMap.IsMouseHovering)
            {
                newCustomMap.BackgroundColor = Main.DiscoColor;
            }

        }
        private void HoverCustomMap(UIMouseEvent evt, UIElement listeningElement)
        {
            UITextPanel<string> targetPanel = evt.Target as UITextPanel<string>;

            if (targetPanel != null)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }



        private void HoverVanillaMap(UIMouseEvent evt, UIElement listeningElement)
        {
            UITextPanel<string> targetPanel = evt.Target as UITextPanel<string>;

            if (targetPanel != null)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);

                targetPanel.BackgroundColor = new Color(73, 94, 171);
                targetPanel.BorderColor = Colors.FancyUIFatButtonMouseOver;

            }
        }
        private void UnselectCustomMap(UIMouseEvent evt, UIElement listeningElement)
        {
            UITextPanel<string> targetPanel = evt.Target as UITextPanel<string>;

            if (targetPanel != null)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                targetPanel.BorderColor = Color.Black;
            }
        }

        private void UnselectVanillaMap(UIMouseEvent evt, UIElement listeningElement)
        {
            UITextPanel<string> targetPanel = evt.Target as UITextPanel<string>;

            if (targetPanel != null)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);

                targetPanel.BackgroundColor = new Color(33, 43, 120);
                targetPanel.BorderColor = Color.Black;
            }
        }

        private void UnselectBackButton(UIMouseEvent evt, UIElement listeningElement)
        {
            UITextPanel<string> targetPanel = evt.Target as UITextPanel<string>;

            if (targetPanel != null)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);

                targetPanel.BackgroundColor = new Color(33, 43, 79);
                targetPanel.BorderColor = Color.Black;
            }
        }

        private void VanillaMapSelected(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            Main.MenuUI.SetState(new UIWorldCreation());
        }
        private void CustomMapSelected(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            UITextPanel<string> targetPanel = evt.Target as UITextPanel<string>;

            string dataDir = Main.SavePath + "\\ModConfigs\\tsorcRevampData";

            string baseMapFileName = "\\tsorcBaseMap.wld";
            string userMapFileName = "\\TheStoryofRedCloud.wld";
            string worldsFolder = Main.SavePath + "\\Worlds";

            Mod mod = ModContent.GetInstance<tsorcRevamp>();
            tsorcRevamp thisMod = (tsorcRevamp)mod;
            if (!thisMod.IsMapInvalid(dataDir + baseMapFileName))
            {
                if (File.Exists(dataDir + baseMapFileName))
                {
                    if (!File.Exists(worldsFolder + userMapFileName))
                    {
                        FileInfo fileToCopy = new FileInfo(dataDir + baseMapFileName);
                        mod.Logger.Info("Attempting to copy world.");

                        try
                        {
                            fileToCopy.CopyTo(worldsFolder + userMapFileName, false);
                        }
                        catch (System.Security.SecurityException e)
                        {
                            mod.Logger.Warn("World copy failed ({0}). Try again with administrator privileges?", e);
                        }
                        catch (Exception e)
                        {
                            mod.Logger.Warn("World copy failed ({0}).", e);
                        }
                    }
                    else
                    {
                        mod.Logger.Info("World already exists. Making renamed copy.");
                        FileInfo fileToCopy = new FileInfo(dataDir + baseMapFileName);
                        try
                        {
                            string newFileName;
                            bool validName = false;
                            int worldCount = 1;
                            do
                            {
                                newFileName = "\\TheStoryOfRedCloud_" + worldCount.ToString() + ".wld";
                                if (File.Exists(worldsFolder + newFileName))
                                {
                                    worldCount++;
                                    if (worldCount > 255)
                                    {
                                        mod.Logger.Warn("World copy failed, too many copies.");
                                    }
                                }
                                else
                                {
                                    validName = true;
                                }
                            } while (!validName);

                            fileToCopy.CopyTo(worldsFolder + newFileName, false);
                        }
                        catch (System.Security.SecurityException e)
                        {
                            mod.Logger.Warn("World copy failed ({0}). Try again with administrator privileges?", e);
                        }
                        catch (Exception e)
                        {
                            mod.Logger.Warn("World copy failed ({0}).", e);
                        }
                    }
                }

                Main.OpenWorldSelectUI();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(LangUtils.GetTextValue("UI.TSORCMapFailed"), "TSORC: Map Download Failure!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //targetPanel.SetText("Error: Map missing or corrupted!\nVisit our discord for help: discord.gg/UGE6Mstrgz");
            }

        }

        private void BackButtonPressed(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            Main.OpenWorldSelectUI();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
        }

    }


    internal class tsorcUICenteredTextButton : UITextPanel<string>
    {

        internal string UIText;

        public tsorcUICenteredTextButton(string text, float textScale, bool large = false) : base("", textScale, large)
        {
            UIText = text;
        }

        public float TextVAlign = 0.5f;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            DrawText(spriteBatch);
        }



        new void DrawText(SpriteBatch spriteBatch)
        {
            CalculatedStyle innerDimensions = GetInnerDimensions();
            Vector2 pos = innerDimensions.Position();
            if (_isLarge)
            {
                pos.Y -= 15f * _textScale * _textScale;
            }
            else
            {
                pos.Y -= 2f * _textScale;
            }

            //Purely to get the proper string sizing
            SetText(UIText);
            Vector2 textDims = TextSize;
            SetText("");

            pos.X += (innerDimensions.Width - textDims.X) * TextHAlign;
            pos.Y += (innerDimensions.Height - textDims.Y) * TextVAlign;

            if (_isLarge)
            {
                Utils.DrawBorderStringBig(spriteBatch, UIText, pos, _color, _textScale);
            }
            else
            {
                Utils.DrawBorderString(spriteBatch, UIText, pos, _color, _textScale);
            }
        }
    }
}