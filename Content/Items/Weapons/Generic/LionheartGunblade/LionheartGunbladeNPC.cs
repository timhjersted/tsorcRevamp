using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Content.Items.Weapons.Generic.LionheartGunblade;

public class LionheartGunbladeNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public int LionheartMarks = 0;
    Texture2D LionheartMarksSprite;
    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (npc.HasBuff(ModContent.BuffType<LionheartMark>()) && LionheartMarks > 0 && !Main.dedServ)
        {
            LionheartMarksSprite = (Texture2D)ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(LionheartMark)));
            Rectangle LionheartSourceRectangle = new Rectangle(0, (5 - LionheartMarks) * LionheartMarksSprite.Height / 5,
                LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
            Main.EntitySpriteDraw(LionheartMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, LionheartMarks * LionheartMarksSprite.Height / 5 - 100),
                LionheartSourceRectangle, Color.White, 0, LionheartSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
        }
        return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
    }
}