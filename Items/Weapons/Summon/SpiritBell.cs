
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon
{
	public class SpiritBell : ModItem
	{
		public override void SetStaticDefaults()
		{
			Tooltip.SetDefault("Summons a raging Phoenix to fight for you" +
                "\nDeals extra damage after warming up, this scales well with crits" +
                "\nUses 2 minion slots");

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
			ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; // This lets the player target anywhere on the whole screen while using a controller
			ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
		}
		public override void SetDefaults()
		{
			Item.damage = 24;
			Item.knockBack = 3f;
			Item.mana = 10;
			Item.width = 32;
			Item.height = 32;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.value = 10000;
			Item.rare = ItemRarityID.Expert;
			Item.expert = true;
			Item.UseSound = SoundID.Item44;


			Item.noMelee = true;
			Item.DamageType = DamageClass.Summon;
			Item.buffType = ModContent.BuffType<Buffs.Summon.PhoenixBuff>();
			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.SpiritBell.BarrowWightMinion>();
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			// Here you can change where the minion is spawned. Most vanilla minions spawn at the cursor position
			position = Main.MouseWorld;
		}
	}
}
