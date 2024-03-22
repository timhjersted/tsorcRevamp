using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class CorruptedTooth : ModItem
    {
        public override void SetStaticDefaults()
        {
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
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Green;
        }
        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (target.type == NPCID.EaterofSouls
                || target.type == NPCID.BigEater
                || target.type == NPCID.LittleEater
                || tsorcRevamp.EaterOfWorldsSegments.Contains(target.type)
                || tsorcRevamp.DevourerSegments.Contains(target.type)
                || target.type == ModContent.NPCType<NPCs.Enemies.BasiliskShifter>()
                )
            {
                modifiers.SourceDamage *= 3;
                player.statLife += modifiers.GetDamage(modifiers.SourceDamage.ApplyTo(Item.damage) / 15, true);
                player.HealEffect(modifiers.GetDamage(modifiers.SourceDamage.ApplyTo(Item.damage) / 15, true));
            }
            else
            {
                player.statLife += modifiers.GetDamage(modifiers.SourceDamage.ApplyTo(Item.damage) / 10, true);
                player.HealEffect(modifiers.GetDamage(modifiers.SourceDamage.ApplyTo(Item.damage) / 10, true));
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.SuperHardMode.GuardianCorruptor>())
            {
                //please *DO* use this on a guardian corruptor!
                modifiers.DamageVariationScale *= 0;
                modifiers.Defense *= 0;
                modifiers.SourceDamage *= 0;
                modifiers.FlatBonusDamage += 500000000f;
                modifiers.SetCrit(); //let it crit!
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.BasiliskWalker>())
            {
                modifiers.SourceDamage *= 2;
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.SuperHardMode.BasiliskHunter>())
            {
                modifiers.SourceDamage *= 11;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverBroadsword);
            recipe.AddIngredient(ItemID.RottenChunk, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
