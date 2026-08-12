---
name: terraria-pixel-art
description: Create or convert Terraria/tModLoader sprites, VFX masks, enemies, projectiles, and tiles into deliberately chunky pixel art. Use when an asset must read at small gameplay scale, preserve a minimum 2x2-pixel brush stroke, avoid AI-generated micro-detail, or be prepared as a crisp PNG for a mod.
---

# Terraria Pixel Art

Create readable small assets first; do not try to paint fine detail at the final display size.

## Visual contract

- Default to a **2x2 final-pixel brush**. Do not introduce isolated 1x1 marks, anti-aliasing, blur, or photographic texture.
- Keep game assets compact. Prefer `<= 200px` on the longest edge; use up to roughly `400px` only when the gameplay object is genuinely large.
- Prioritize silhouette, large value groups, and a few material-defining shapes over surface noise.
- Use a limited palette. Reserve the brightest color for focal edges, cracks, impacts, or a small core.
- Match Terraria's side-view perspective. Avoid floor-plane perspective, realistic lighting, and smooth concept-art rendering.

## Reliable generation workflow

1. Write a concrete asset brief: gameplay role, final dimensions, side-view orientation, silhouette, palette, and what must not appear.
2. When an image model is useful, generate a **concept source** on a flat chroma-key background. Say explicitly: chunky pixel art, limited palette, no anti-aliasing, no single-pixel speckles, no text, and generous padding.
3. Inspect the source. Reject it if the silhouette, perspective, or material language is wrong; never use a concept image unchanged as game art.
4. Remove the chroma-key background with the installed ImageGen helper. Confirm transparent corners and no colored fringe.
5. Crop to the opaque bounds. Reduce to half the desired final dimensions with **nearest-neighbor** sampling, then scale it 2x with **nearest-neighbor** again.

   Example: make a 128x224 final monolith by reducing its cropped source to 64x112, then enlarging that result to 128x224. Every deliberate mark becomes a 2x2 block.

6. Inspect at native size and 2x/4x. Check that the silhouette remains recognizable and that both top and bottom edges are intentionally shaped when the object calls for it.
7. Save a non-destructive sibling asset. Do not overwrite an existing sprite without explicit approval.

## VFX mask workflow

For a shader-driven effect, give the sprite only the job it does best: crisp silhouette and stable stone, blade, hand, or rune detail. Let the shader provide moving light, scrolling fire, erosion, glow, and color modulation.

- Use `SamplerState.PointClamp` for the sprite mask so its 2x2 blocks stay crisp.
- Use a separate wrapped noise sampler for moving material data.
- Keep the opaque pixel-art object visually stable; animate the light *inside* it rather than scrolling the whole sprite like fabric.
- Draw the exact damaging core or seam separately when the sprite is decorative or wider than collision.

## Ready-to-use chroma-key prompt additions

```text
Style: compact Terraria-style pixel art. Use deliberately chunky 2x2-pixel minimum brush marks,
a limited palette, no anti-aliasing, no smoothing, no single-pixel speckle, no painterly gradients.
Background: perfectly flat solid #00ff00 chroma-key background with no shadow, floor, or gradient.
Composition: side-view, centered, generous empty padding, no text or watermark.
```

## Verification

- Confirm the output PNG has alpha and transparent corners.
- Confirm final dimensions and that the reduced-then-doubled workflow was used for the intended 2x2 look.
- Inspect at 1x plus an integer multiple; do not judge only from a blurred preview.
- For mod assets, verify the extensionless `ModContent.Request<Texture2D>` path and build the mod.
- For shader-backed assets, preview the real sprite with the same shader math before shipping.