using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using System;
using System.Net;
using Newtonsoft.Json;
using System.Threading;

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
			Main.NewText(player.position / 16);
			
			/*
			string dataDir = Main.SavePath + "\\Mod Configs\\tsorcRevampData";
			if (!Directory.Exists(dataDir))
            {
				try
				{
					Main.NewText("Creating directory " + dataDir);
					Directory.CreateDirectory(dataDir);
				}
				catch (Exception e)
				{
					Main.NewText("Failed to create directory " + dataDir + "!!", Color.Orange);
					Main.NewText(e);
				}
            }

			try
			{
				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
				using (WebClient client = new WebClient())
				{
					client.DownloadFileAsync(new Uri(VariousConstants.MAP_URL), dataDir + "\\tsorcBaseMap038.wld");
				}
			}
			catch (Exception e)
			{
				Main.NewText("Failed to download map!!", Color.Orange);
				Main.NewText(e);
			}

			string musicModDir = Main.SavePath + "\\Mods\\tsorcMusic.tmod";
			try
			{
				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
				using (WebClient client = new WebClient())
				{
					client.DownloadFileAsync(new Uri(VariousConstants.MUSIC_MOD_URL), musicModDir);
				}
			}
			catch (Exception e) //https://imgur.com/a/YHrhvRY.png
			{
				Main.NewText("Failed to download music mod!!", Color.Orange);
				Main.NewText(e);
			}*/
			return true;
		}



		public override bool CanUseItem(Player player)
        {
			if (player.name == "Zeodexic" || player.name.Contains("Sam") || player.name == "Chroma TSORC test")
			{
				return true;
			}
			return false;
		}
	}
}