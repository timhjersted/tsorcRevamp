// Demon family fireball (Content/Projectiles/Enemy/DemonFamily/DemonProjectiles.cs). One shape, three palettes set
// from C#: T1 orange -> yellow core, T2 black rim -> pink core, T3 black rim -> bright red core.
// Drawn premultiplied under BlendState.AlphaBlend, so a near-black DarkColor still reads: alpha darkens what's behind.
// Shape = MinotaurFirebolt's rounded-nose bolt; the positive-Y end leads and the noise flows toward the -Y tail.
sampler NoiseSampler : register(s0);
float Time;
float Opacity;
float Seed;
float4 PixelGrid;   // xy = 2px block count across the quad, zw = its reciprocal (pre-divided in C#)
float3 DarkColor;   // outer flame body
float3 MidColor;    // heat band, arrives with body^2
float3 CoreColor;   // hot centre near the nose

float4 FireballPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = (floor(coords * PixelGrid.xy) + 0.5) * PixelGrid.zw;
    float noise = tex2D(NoiseSampler, uv * float2(1.8, 0.65) + float2(Seed, Time * 1.1)).r;
    float detail = tex2D(NoiseSampler, uv * float2(3.7, 1.1) + float2(-Seed, Time * 1.6)).r;
    float across = abs(uv.x - 0.5);

    // Tail widens toward the nose; width <= .40 so the sides never reach x=0/1.
    float tail = saturate((uv.y - 0.06) * 1.4);
    float width = (0.10 + tail * 0.30) * (0.78 + noise * 0.22);
    float edge = saturate((width - across) * 16.0);

    // Rounded nose: zero above y=.93, curving back into the body.
    float cap = saturate((0.93 - uv.y - across * across * 1.8) * 12.0);
    float body = edge * cap * tail * saturate(noise * 1.1 + detail * 0.6);
    float heat = body * body;
    float core = heat * saturate(1.0 - abs(uv.y - 0.70) * 4.0) * saturate((0.22 - across) * 7.0);

    float3 color = DarkColor * body + MidColor * heat + CoreColor * core;
    float alpha = saturate(body * 1.6) * Opacity;
    return float4(color * Opacity * vertexColor.rgb, alpha * vertexColor.a);
}

technique DemonFireball
{
    pass FireballPass { PixelShader = compile ps_2_0 FireballPixel(); }
}
