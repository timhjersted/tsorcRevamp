using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.UI
{
    class StartingClassUIState : UIState
    {
        public override void OnInitialize()
        {
            ConstructUI();
        }

        private void ConstructUI()
        {
            RemoveAllChildren();

            UIElement selector = new UIElement
            {
                Width = StyleDimension.FromPixels(400f),
                Height = StyleDimension.FromPixels(475f),
                Top = StyleDimension.FromPixels(0f),
                HAlign = 0.5f,
                VAlign = 0.5f,
            };

            tsorcUICenteredTextButton background = new tsorcUICenteredTextButton(LangUtils.GetTextValue("UI.ChooseStartingClass"), .425f, true)
            {
                Width = StyleDimension.FromPercent(1f),
                Height = StyleDimension.FromPixels(235),
                Top = StyleDimension.FromPixels(50f),
                TextVAlign = 0.06f,
                BackgroundColor = new Color(33, 43, 79) * 0.8f
            };

            AddClassButton(background, StartingClass.Melee, "UI.StartingClassMelee", 40f);
            AddClassButton(background, StartingClass.Ranged, "UI.StartingClassRanged", 79f);
            AddClassButton(background, StartingClass.Magic, "UI.StartingClassMagic", 118f);
            AddClassButton(background, StartingClass.Summoner, "UI.StartingClassSummoner", 157f);

            selector.Append(background);

            tsorcUICenteredTextButton backButton = new tsorcUICenteredTextButton(LangUtils.GetTextValue("UI.Back"), .425f, true)
            {
                Width = StyleDimension.FromPercent(1f),
                Height = StyleDimension.FromPixels(40),
                Top = StyleDimension.FromPixels(287.5f),
                BackgroundColor = new Color(33, 43, 79),
                TextHAlign = 0.5f
            };

            backButton.OnMouseOver += HoverBackButton;
            backButton.OnMouseOut += UnhoverBackButton;
            backButton.OnLeftMouseDown += BackButtonPressed;
            selector.Append(backButton);

            Append(selector);
        }

        private void AddClassButton(UIElement parent, StartingClass startingClass, string localizationKey, float top)
        {
            tsorcUICenteredTextButton button = new tsorcUICenteredTextButton(LangUtils.GetTextValue(localizationKey), .425f, true)
            {
                Width = StyleDimension.FromPercent(1f),
                Height = StyleDimension.FromPixels(31.5f),
                Top = StyleDimension.FromPixels(top),
                HAlign = 0.5f,
                TextVAlign = 0.5f,
                BackgroundColor = new Color(33, 43, 120)
            };

            button.OnMouseOver += HoverButton;
            button.OnMouseOut += UnhoverButton;
            button.OnLeftMouseDown += (evt, listeningElement) => SelectClass(startingClass);
            parent.Append(button);
        }

        private void SelectClass(StartingClass startingClass)
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            tsorcRevamp.PendingStartingClass = startingClass;
            Main.MenuUI.SetState(new UICharacterCreation(new Player()));
        }

        private void BackButtonPressed(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            tsorcRevamp.PendingStartingClass = StartingClass.None;
            Main.OpenCharacterSelectUI();
        }

        private void HoverButton(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            if (evt.Target is tsorcUICenteredTextButton targetPanel)
            {
                targetPanel.BackgroundColor = new Color(33, 43, 120).MultiplyRGB(Main.DiscoColor * 0.9f);
                targetPanel.BorderColor = Color.White;
            }
        }

        private void UnhoverButton(UIMouseEvent evt, UIElement listeningElement)
        {
            if (evt.Target is tsorcUICenteredTextButton targetPanel)
            {
                targetPanel.BackgroundColor = new Color(33, 43, 120);
                targetPanel.BorderColor = Color.Black;
            }
        }
        private void HoverBackButton(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            if (evt.Target is tsorcUICenteredTextButton targetPanel)
            {
                targetPanel.BackgroundColor = new Color(73, 94, 171);
                targetPanel.BorderColor = Colors.FancyUIFatButtonMouseOver;
            }
        }

        private void UnhoverBackButton(UIMouseEvent evt, UIElement listeningElement)
        {
            if (evt.Target is tsorcUICenteredTextButton targetPanel)
            {
                targetPanel.BackgroundColor = new Color(33, 43, 79);
                targetPanel.BorderColor = Color.Black;
            }
        }
    }
}
