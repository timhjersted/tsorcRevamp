using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class GoldenHairpin : ModItem
    {
        public int AmmoType = 0;
        public float DmgMult = 20f;
        public const int SwitchTiming = 7; //every X seconds
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DmgMult, SwitchTiming);
        public override void SetStaticDefaults()
        {

        }

        public override void SetDefaults()
        {

            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.expert = true;
            Item.value = PriceByRarity.Cyan_9;
        }
        public override void UpdateEquip(Player player)
        {
            Rectangle PlayerRect = Utils.CenteredRectangle(player.Center, player.Size);
            AmmoType++;
            switch (AmmoType)
            {
                case SwitchTiming * 60 * 0:
                    {
                        player.arrowDamage *= 1f + DmgMult / 100f;
                        SoundEngine.PlaySound(SoundID.Item102 with { Volume = 3f });
                        CombatText.NewText(PlayerRect, Color.BurlyWood, LaUtils.GetTextValue("Items.GoldenHairpin.Bow"));
                        break;
                    }
                case int Arrow when (Arrow > 0 && Arrow <= SwitchTiming * 60 - 1):
                    {
                        player.arrowDamage *= 1f + DmgMult / 100f;
                        break;
                    }
                case SwitchTiming * 60 * 1:
                    {
                        player.bulletDamage *= 1f + DmgMult / 100f;
                        SoundEngine.PlaySound(SoundID.Item149 with { Volume = 3f });
                        CombatText.NewText(PlayerRect, Color.BurlyWood, LaUtils.GetTextValue("Items.GoldenHairpin.Gun"));
                        break;
                    }
                case int Bullet when (Bullet > SwitchTiming * 60 * 1 && Bullet <= SwitchTiming * 60 * 2 - 1):
                    {
                        player.bulletDamage *= 1f + DmgMult / 100f;
                        break;
                    }
                case SwitchTiming * 60 * 2:
                    {
                        player.specialistDamage *= 1f + DmgMult / 100f;
                        SoundEngine.PlaySound(SoundID.Item14 with { Volume = 3f });
                        CombatText.NewText(PlayerRect, Color.BurlyWood, LaUtils.GetTextValue("Items.GoldenHairpin.Specialist"));
                        break;
                    }
                case int Specialist when (Specialist >= SwitchTiming * 60 * 2 && Specialist <= SwitchTiming * 60 * 3 - 1):
                    {
                        player.specialistDamage *= 1f + DmgMult / 100f;
                        break;
                    }
                case int Reset when (Reset > SwitchTiming * 60 * 3):
                    {
                        AmmoType = -1;
                        break;
                    }
            }
        }
    }
}
