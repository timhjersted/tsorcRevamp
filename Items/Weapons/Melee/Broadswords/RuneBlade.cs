using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class RuneBlade : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("A sword used to kill magic users." +
                                "\n[c/ffbf00:Does up to 8x damage to mages]" +
                                "\nEvery third hit with the Blade generates a magic slash"); */
        }
        public int shootstacks = 0;
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.damage = 20;
            Item.height = 36;
            Item.knockBack = 5;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.value = PriceByRarity.Green_2;
            Item.width = 36;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            Item.shootSpeed = 7.5f;
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            shootstacks += 1;
            if (shootstacks >= 3)
            {
                Projectile.NewProjectile(Projectile.GetSource_None(), player.Center, Main.MouseWorld - player.Center, ProjectileID.DD2SquireSonicBoom, damageDone * 3, hit.Knockback, Main.myPlayer);
                shootstacks = 0;
            }
        }
        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (target.type == NPCID.DarkCaster
                || target.type == NPCID.GoblinSorcerer
                || target.type == ModContent.NPCType<UndeadCaster>()
                || target.type == ModContent.NPCType<MindflayerServant>()
                )
            {
                modifiers.FinalDamage *= 8;
            }
            if (target.type == NPCID.Tim
                || target.type == ModContent.NPCType<DungeonMage>()
                || target.type == ModContent.NPCType<DemonSpirit>()
                || target.type == ModContent.NPCType<ShadowMage>()
                || target.type == ModContent.NPCType<AttraidiesIllusion>()
                || target.type == ModContent.NPCType<AttraidiesManifestation>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.AttraidiesMimic>()
                || target.type == ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>()
                )
            {
                modifiers.FinalDamage *= 4;
            }
            if (target.type == ModContent.NPCType<CrazedDemonSpirit>()

                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DarkShogunMask>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.SecondForm.DarkDragonMask>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.ThirdForm.Okiku>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.ThirdForm.BrokenOkiku>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>()

                || target.type == ModContent.NPCType<NPCs.Enemies.MindflayerKingServant>()
                || target.type == ModContent.NPCType<NPCs.Enemies.MindflayerServant>()
                || target.type == ModContent.NPCType<NPCs.Enemies.MindflayerIllusion>()
                || target.type == ModContent.NPCType<NPCs.Bosses.Fiends.LichKingDisciple>()
                )
            {
                modifiers.FinalDamage *= 8;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.LightsBane);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
