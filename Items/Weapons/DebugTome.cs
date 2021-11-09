using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
	public class DebugTome : ModItem {
		public override void SetStaticDefaults() {
			Tooltip.SetDefault("You should not have this" +
				"\nDev item used for testing purposes only" +
				"\nUsing this may cause irreversible effects on your world");
		}
		
		public override void SetDefaults() {
			item.damage = 999999;
			item.knockBack = 4;
			item.crit = 4;
			item.width = 30;
			item.height = 30;
			item.useTime = 20;
			item.useAnimation = 20;
			item.UseSound = SoundID.Item11;
			item.useTurn = true;
			item.noMelee = true;
			item.magic = true;
			item.autoReuse = true;
			item.value = 10000;
			item.useStyle = ItemUseStyleID.HoldingOut;
			item.rare = ItemRarityID.Green;
			item.shootSpeed = 24f;
			item.shoot = ModContent.ProjectileType<Projectiles.BlackFirelet>();
		}

		public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack) {
			tsorcRevampWorld.SuperHardMode = true;
			Main.dayTime = false;
			Main.time = 13000;
			Main.NewText(player.position / 16);
			return true;
		}
		public override bool CanUseItem(Player player)
        {
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				Main.NewText("client");
			}
			if (Main.netMode == NetmodeID.Server)
			{
				NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("server"), Color.Blue);
			}
			if (player.name == "Zeodexic" || player.name.Contains("Sam") || player.name == "Chroma TSORC test")
			{
				return true;
			}
			return false;
		}
	}
}