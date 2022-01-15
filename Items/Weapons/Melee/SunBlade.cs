using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class SunBlade : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A sword used to kill the undead" +
                                "\nDoes 4x damage against the heartless");
        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.damage = 25;
            item.height = 36;
            item.knockBack = 9;
            item.maxStack = 1;
            item.melee = true;
            item.useAnimation = 23;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = PriceByRarity.LightRed_4;
            item.width = 36;
        }

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            //todo add mod NPCs to this list
            if (NPCID.Sets.Skeletons.Contains(target.type)
                    || target.type == NPCID.Zombie
                    || target.type == NPCID.BaldZombie
                    || target.type == NPCID.AngryBones
                    || target.type == NPCID.DarkCaster
                    || target.type == NPCID.CursedSkull
                    || target.type == NPCID.UndeadMiner
                    || target.type == NPCID.Tim
                    || target.type == NPCID.DoctorBones
                    || target.type == NPCID.ArmoredSkeleton
                    || target.type == NPCID.Mummy
                    || target.type == NPCID.DarkMummy
                    || target.type == NPCID.LightMummy
                    || target.type == NPCID.Wraith
                    || target.type == NPCID.SkeletonArcher
                    || target.type == NPCID.PossessedArmor
                    || target.type == NPCID.TheGroom
                    || target.type == NPCID.SkeletronHand
                    || target.type == NPCID.SkeletronHead
                    || target.type == ModContent.NPCType<NPCs.Bosses.Fiends.LichKingSerpentHead>()
                    || target.type == ModContent.NPCType<NPCs.Bosses.Fiends.LichKingSerpentBody>()
                    || target.type == ModContent.NPCType<NPCs.Bosses.Fiends.LichKingSerpentTail>()
                    || target.type == ModContent.NPCType<NPCs.Enemies.DemonSpirit>()
                    || target.type == ModContent.NPCType<NPCs.Enemies.CrazedDemonSpirit>()
                    || target.type == ModContent.NPCType<NPCs.Enemies.ZombieWormHead>()
                    || target.type == ModContent.NPCType<NPCs.Enemies.ZombieWormBody>()
                    || target.type == ModContent.NPCType<NPCs.Enemies.ZombieWormTail>()
                    )
            {
                damage *= 4;
            }
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(ItemID.LightsBane);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

    }
}
