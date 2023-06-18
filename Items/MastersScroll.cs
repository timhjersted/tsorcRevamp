using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    public class MastersScroll : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 26;
            Item.useTime = 100;
            Item.useAnimation = 100;
            Item.UseSound = SoundID.Item11;
            Item.useTurn = true;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.value = 0;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shoot = ProjectileID.Ale;
            Item.rare = ItemRarityID.Master;
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.GameMode == 1)
                {
                    Main.GameMode = 2;
                    Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.MastersScroll.Enabled"), Color.DarkRed);
                }
                else
                {
                    Main.GameMode = 1;
                    Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.MastersScroll.Disabled"), Color.DarkRed);
                }
            }
            else
            {
                if (player.whoAmI == Main.LocalPlayer.whoAmI)
                {
                    ModPacket timePacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                    timePacket.Write(tsorcPacketID.SyncMasterScroll);
                    timePacket.Write(Main.GameMode);
                    timePacket.Send();
                }
            }

            return false;
        }
    }
}