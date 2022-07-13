using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class HeavensTear : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Heaven's Tear");
            Tooltip.SetDefault("'Heaven splits with each swing'");
            //"Does 2x damage to mages and ghosts...");

        }

        public override void SetDefaults()
        {

            Item.width = 32;
            Item.height = 32;
            //item.pretendType=389;
            //item.prefixType=368;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.useAnimation = 38;
            Item.useTime = 38;
            Item.maxStack = 1;
            Item.damage = 1000;
            Item.knockBack = (float)10;
            Item.scale = (float)1;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = (float)14;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.HeavensTearBall>();
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.FlowerPow, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("GuardianSoul").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("CursedSoul").Type, 20);
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfArtorias").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 120000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

 //       public override void ModifyHitNPC(Player player, NPC npc, ref int damage, ref float knockBack, ref bool crit)
    //    {
            //damage = (int) ((Main.rand.Next(26)) * (P.GetDamage(DamageClass.Melee)));
      //      if (npc.FullName == "Tim") damage *= 2;
        //    else if (npc.FullName == "Dark Caster") damage *= 2;
          //  else if (npc.FullName == "Goblin Sorcerer") damage *= 2;
            //else if (npc.FullName == "Undead Caster") damage *= 2;
            //else if (npc.FullName == "Mindflayer Servant") damage *= 2;
            //else if (npc.FullName == "Dungeon Mage") damage *= 2;
            //else if (npc.FullName == "Demon Spirit") damage *= 2;
            //else if (npc.FullName == "Crazed Demon Spirit") damage *= 2;
            //else if (npc.FullName == "Shadow Mage") damage *= 2;
            //else if (npc.FullName == "Attraidies Illusion") damage *= 2;
            //else if (npc.FullName == "Attraidies Manifestation") damage *= 2;
            //else if (npc.FullName == "Mindflayer King") damage *= 2;
            //else if (npc.FullName == "Dark Shogun Mask") damage *= 2;
            //else if (npc.FullName == "Dark Dragon Mask") damage *= 2;
            //else if (npc.FullName == "Broken Okiku") damage *= 2;
            //else if (npc.FullName == "Okiku") damage *= 2;
            //else if (npc.FullName == "Wyvern Mage") damage *= 2;
            //else if (npc.FullName == "Ghost of the Forgotten Knight") damage *= 2;
            //else if (npc.FullName == "Ghost of the Forgotten Warrior") damage *= 2;
            //else if (npc.FullName == "Barrow Wight Nemesis") damage *= 2;
            //else if (npc.FullName == "Oolacile Sorcerer") damage *= 2;
            //else if (npc.FullName == "Abysmal Oolacile Sorcerer") damage *= 2;
            //else if (npc.FullName == "Dark Cloud") damage *= 2;
            //else if (npc.FullName == "Barrow Wight") damage *= 2;
    //    }

    }
}
