using Terraria.GameContent.Creative;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Projectiles.Melee.Spears;
using tsorcRevamp.Projectiles.Summon.NullSprite;
using Microsoft.Xna.Framework;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Summon.ForgottenImp;

namespace tsorcRevamp.Items.Weapons.Summon
{
    class ForgottenImpHalberd : ModdedMinionItem
    {
        public override int Width => 54;
        public override int Height => 54;
        public override int MinionProjectileType => ModContent.ProjectileType<ForgottenImpProjectile>();
        public override int MinionBuffType => ModContent.BuffType<ForgottenImpBuff>();
        public const int BaseDamage = 23;
        public override int BaseDmg => BaseDamage;
        public override int Crit => 0;
        public const float RequiredSlots = 1f;
        public override float SlotsRequired => RequiredSlots;
        public const float SummonTagDmgMult = 100f;
        public override float SummonTagDmgMulti => SummonTagDmgMult;
        public override int UseTimeAnimation => 30;
        public override int Mana => 10;
        public override float Knockback => 1f;
        public override int Rarity => ItemRarityID.LightPurple;
        public const int BleedProcBaseDmg = BaseDamage * 4;
        public const int BleedDuration = 5;
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.ImpStaff);
            recipe.AddIngredient(ModContent.ItemType<ImpHead>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
