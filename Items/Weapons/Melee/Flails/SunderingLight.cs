using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Flails
{
    [LegacyName("HeavensTear2")]
    public class SunderingLight : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Sundering Light");
            /* Tooltip.SetDefault("Rips apart the border of life and death" +
                "\nDeals double damage to mages and ghosts"); */

        }

        public override void SetDefaults()
        {

            Item.width = 36;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.maxStack = 1;
            Item.damage = 400;
            Item.knockBack = 10;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Purple;
            Item.shootSpeed = 16;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Flails.SunderingLightBall>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<HeavensTear>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Humanity>(), 10);
            recipe.AddIngredient(ModContent.ItemType<GhostWyvernSoul>());
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 220000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            //damage = (int) ((Main.rand.Next(26)) * (P.GetDamage(DamageClass.Melee)));
            if (   target.FullName == "Tim" 
                || target.FullName == "Dark Caster" 
                || target.FullName == "Goblin Sorcerer" 
                || target.FullName == "Undead Caster"
                || target.FullName == "Mindflayer Servant"
                || target.FullName == "Dungeon Mage"
                || target.FullName == "Demon Spirit"
                || target.FullName == "Crazed Demon Spirit"
                || target.FullName == "Shadow Mage"
                || target.FullName == "Attraidies Illusion"
                || target.FullName == "Attraidies Manifestation"
                || target.FullName == "Mindflayer King"
                || target.FullName == "Dark Shogun Mask"
                || target.FullName == "Dark Dragon Mask"
                || target.FullName == "Broken Okiku"
                || target.FullName == "Okiku"
                || target.FullName == "Wyvern Mage"
                || target.FullName == "Ghost of the Forgotten Knight"
                || target.FullName == "Barrow Wight Nemesis"
                || target.FullName == "Oolacile Sorcerer"
                || target.FullName == "Abysmal Oolacile Sorcerer"
                || target.FullName == "Dark Cloud"
                || target.FullName == "Barrow Wight"
                ) modifiers.FinalDamage *= 2;
        }

    }
}
