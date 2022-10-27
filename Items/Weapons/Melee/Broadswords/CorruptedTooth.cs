using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class CorruptedTooth : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A green ooze dribbles from the tooth, which deals" +
                                "\nquadruple damage to enemies of a similar nature." +
                                "\nHas a chance to heal the player on hit." +
                                "\nHeal chance is doubled on non-corrupted enemies.");
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 21;
            Item.autoReuse = true;
            Item.useTime = 21;
            Item.damage = 15;
            Item.knockBack = 4f;
            Item.UseSound = SoundID.Item1;
            Item.value = PriceByRarity.Blue_1;
            Item.DamageType = DamageClass.Melee;
            Item.rare = ItemRarityID.Blue;
        }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit)
        {
            if (target.type == NPCID.EaterofSouls
                || target.type == NPCID.BigEater
                || target.type == NPCID.LittleEater
                || target.type == NPCID.EaterofWorldsHead
                || target.type == NPCID.EaterofWorldsBody
                || target.type == NPCID.EaterofWorldsTail
                )
            {
                damage *= 4;
                if (Main.rand.NextBool(20))
                {
                    player.statLife += damage;
                    player.HealEffect(damage);
                }
            }
            else
            {
                if (Main.rand.NextBool(10))
                {
                    player.statLife += damage;
                    player.HealEffect(damage);
                }
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.SuperHardMode.GuardianCorruptor>())
            {
                //please *DO* use this on a guardian corruptor!
                crit = false;
                damage = 100074;//reduced to 99999 after defense
            }
            
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverBroadsword);
            recipe.AddIngredient(ItemID.RottenChunk, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 800);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
