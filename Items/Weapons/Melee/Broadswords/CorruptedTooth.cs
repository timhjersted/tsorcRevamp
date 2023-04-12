using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class CorruptedTooth : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("[c/ffbf00:A green ooze dribbles from the tooth, which deals]" +
                                "\n[c/ffbf00:triple damage to enemies of a similar nature, potentially more.]" +
                                "\nHas a chance to heal the player on hit." +
                                "\nHeal chance is doubled on non-corrupted enemies."); */
        }
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
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
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }
        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (target.type == NPCID.EaterofSouls
                || target.type == NPCID.BigEater
                || target.type == NPCID.LittleEater
                || target.type == NPCID.EaterofWorldsHead
                || target.type == NPCID.EaterofWorldsBody
                || target.type == NPCID.EaterofWorldsTail
                || target.type == ModContent.NPCType<NPCs.Enemies.BasiliskShifter>()
                )
            {
                modifiers.FinalDamage *= 3;
                if (Main.rand.NextBool(20))
                {
                    player.statLife += modifiers.GetDamage(Item.damage, true);
                    player.HealEffect(modifiers.GetDamage(Item.damage, true));
                }
            }
            else
            {
                if (Main.rand.NextBool(10))
                {
                    player.statLife += modifiers.GetDamage(Item.damage, true);
                    player.HealEffect(modifiers.GetDamage(Item.damage, true));
                }
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.SuperHardMode.GuardianCorruptor>())     
            {
                //please *DO* use this on a guardian corruptor!
                modifiers.SetCrit(); //let it crit!
                modifiers.FlatBonusDamage += 100074;//reduced to 99999 after defense
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.BasiliskWalker>())
            {
                modifiers.FinalDamage *= 2;
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.SuperHardMode.BasiliskHunter>())
            {
                modifiers.FinalDamage *= 11;
            }

        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverBroadsword);
            recipe.AddIngredient(ItemID.RottenChunk, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
