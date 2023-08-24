using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class WereBane : ModItem
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("WereBane");
            /* Tooltip.SetDefault("A sword used to kill werewolves instantly." +
                                "\nDoes 8x damage to werewolves."); */
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.damage = 32;
            Item.width = 42;
            Item.height = 42;
            Item.knockBack = 9;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.useTime = 21;
            Item.useAnimation = 21;
            Item.value = PriceByRarity.LightRed_4;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Gold;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GoldBroadsword, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 111);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (target.type == NPCID.Werewolf)
            {
                modifiers.FinalDamage *= 16;
            }
        }
    }
}
