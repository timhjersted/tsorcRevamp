using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.Projectiles.Melee;
using tsorcRevamp.Projectiles.Melee.Broadswords;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class RuneBlade : ModItem
    {
        public int RuneRange = 150;
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.height = 44;
            Item.width = 44;
            Item.rare = ItemRarityID.Green;
            Item.damage = 18;
            Item.knockBack = 5;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Green_2;
            Item.shoot = ModContent.ProjectileType<RuneBladeRune>();
            Item.shootSpeed = 0;
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Cyan;

        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            damage /= 2;
            position = player.Center + Main.rand.NextVector2Circular(RuneRange, RuneRange);
        }

        public override void MeleeEffects(Player player, Rectangle rectangle)
        {
            Vector2 difference = new Vector2(rectangle.X, rectangle.Y) - Main.MouseWorld; //Distance between the sword rectangle and mouse in the world
            Vector2 spawnPosition = new Vector2(30, 0).RotatedBy(difference.ToRotation()); //30 is the distance we will spawn the dusts away from the swords rectangle
            for (int i = 0; i < 2; i++)                                                   //I-m drawing the dusts like this because they spawn all over the player otherwise, this separates them from the player
            {
                Dust RuneDust = Dust.NewDustDirect(new Vector2(rectangle.X - 6, rectangle.Y - 6) - spawnPosition, 1, 1, 90, player.velocity.X, player.velocity.Y, 150, Color.Red, 1f); // -6, -6 because it doesnt actually seem to be centered otherwise
                RuneDust.noGravity = true;
                RuneDust.velocity.Y *= 0;
                RuneDust.velocity.X *= 0;

            }

            /*for (int i = 0; i < 30; i++) //using this to spawn dusts makes dusts spawn on the player
            {
                int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), 10, 10, 90, 0, 0, 150, color, 1f);
                Main.dust[dust].noGravity = true;
            }*/
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
                || target.type == ModContent.NPCType<MountedSandsprogMage>()
                || target.type == ModContent.NPCType<SandsprogMage>()
                || target.type == ModContent.NPCType<Necromancer>()
                || target.type == ModContent.NPCType<NecromancerElemental>()
                || target.type == ModContent.NPCType<Warlock>()
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
                || target.type == ModContent.NPCType<LichKingDisciple>()
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
