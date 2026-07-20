namespace tsorcRevamp.NPCs.EnemySpriteRendering
{
    /// <summary>
    /// Implemented by every <see cref="Terraria.ModLoader.ModNPC"/> that paints itself
    /// through an <see cref="EnemySpriteRenderer"/>. Lets the
    /// <see cref="tsorcPacketID.SyncEnemyActivePose"/> packet handler locate the
    /// renderer for an arbitrary NPC index when receiving a server-broadcast active-pose
    /// trigger (throw release, magic release, etc.).
    ///
    /// Just expose the renderer field — no other contract.
    /// </summary>
    public interface IEnemySpriteRendererHost
    {
        EnemySpriteRenderer SpriteRenderer { get; }
    }
}
