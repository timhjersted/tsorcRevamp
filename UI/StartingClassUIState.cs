using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using tsorcRevamp.Items.Accessories;
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
                Height = StyleDimension.FromPixels(514f),
                Top = StyleDimension.FromPixels(0f),
                HAlign = 0.5f,
                VAlign = 0.5f,
            };

            tsorcUICenteredTextButton background = new tsorcUICenteredTextButton(LangUtils.GetTextValue("UI.ChooseStartingClass"), .425f, true)
            {
                Width = StyleDimension.FromPercent(1f),
                Height = StyleDimension.FromPixels(274),
                Top = StyleDimension.FromPixels(50f),
                TextVAlign = 0.06f,
                BackgroundColor = new Color(33, 43, 79) * 0.8f
            };

            AddClassButton(background, StartingClass.Melee, "UI.StartingClassMelee", 40f);
            AddClassButton(background, StartingClass.Ranged, "UI.StartingClassRanged", 79f);
            AddClassButton(background, StartingClass.Magic, "UI.StartingClassMagic", 118f);
            AddClassButton(background, StartingClass.Summoner, "UI.StartingClassSummoner", 157f);
            AddClassButton(background, StartingClass.Deprived, "UI.StartingClassDeprived", 196f);

            selector.Append(background);

            tsorcUICenteredTextButton backButton = new tsorcUICenteredTextButton(LangUtils.GetTextValue("UI.Back"), .425f, true)
            {
                Width = StyleDimension.FromPercent(1f),
                Height = StyleDimension.FromPixels(40),
                Top = StyleDimension.FromPixels(326.5f),
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
            ClassSelectButton button = new ClassSelectButton(LangUtils.GetTextValue(localizationKey), .425f, true)
            {
                Width = StyleDimension.FromPercent(1f),
                Height = StyleDimension.FromPixels(31.5f),
                Top = StyleDimension.FromPixels(top),
                HAlign = 0.5f,
                TextVAlign = 0.5f,
                BackgroundColor = new Color(33, 43, 120),
                TooltipText = BuildClassTooltip(startingClass)
            };

            button.OnMouseOver += HoverButton;
            button.OnMouseOut += UnhoverButton;
            button.OnLeftMouseDown += (evt, listeningElement) => SelectClass(startingClass);
            parent.Append(button);
        }

        // tsorcUICenteredTextButton's normal hover pattern (Main.hoverItemName) only gets drawn by the in-game
        // HUD's DrawInterface_33_MouseText, which never runs while Main.MenuUI is showing this screen. tML's
        // per-frame TooltipMouseText cache (drawn at the tail of Main.DrawMenu) is the menu-context equivalent,
        // so this subclass re-asserts it every frame while hovered instead of relying on a one-shot mouse event.
        private class ClassSelectButton : tsorcUICenteredTextButton
        {
            public string TooltipText;

            public ClassSelectButton(string text, float textScale, bool large = false) : base(text, textScale, large) { }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);

                if (IsMouseHovering && !string.IsNullOrEmpty(TooltipText))
                {
                    UICommon.TooltipMouseText(TooltipText);
                }
            }
        }

        // Stats + starting weapons + Norman's Ring benefit for the given class, shown as a mouse tooltip while
        // hovering its button. Pulled live from tsorcRevampPlayer/NormansRing rather than hardcoded so this can
        // never drift out of sync with what the class actually grants at character creation.
        private static string BuildClassTooltip(StartingClass startingClass)
        {
            int life = tsorcRevampPlayer.GetBaseLifeForClass(startingClass);
            int mana = tsorcRevampPlayer.GetBaseManaForClass(startingClass);
            int stamina = (int)tsorcRevampPlayer.GetBaseStaminaForClass(startingClass);

            int[] weaponTypes = tsorcRevampPlayer.GetClassStartingWeaponTypes(startingClass);
            string weapons = weaponTypes.Length > 0
                ? string.Join(", ", weaponTypes.Select(Lang.GetItemNameValue))
                : LangUtils.GetTextValue("UI.StartingClassNoWeapons");

            string ringBonus = NormansRing.GetBonusDescription(startingClass);

            return LangUtils.GetTextValue("UI.StartingClassTooltip", life, mana, stamina, weapons, ringBonus);
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
