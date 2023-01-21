float2 uTargetPosition;
float2 uScreenPosition;
float2 uScreenResolution;
float4 uColor;
float uProgress;
float uOpacity;
sampler uImage0 : register(s0);

//Heavily inspired by Kazzymodus's excellent tutorial: https://forums.terraria.org/index.php?threads/tutorial-shockwave-effect-for-tmodloader.81685/
float4 PixelShaderFunction(float4 position : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{
    float PI = 3.14159;
    float2 targetCoords = (uTargetPosition - uScreenPosition) / uScreenResolution;
    float2 centreCoords = (coords - targetCoords) * (uScreenResolution / uScreenResolution.y);
    float dotField = dot(centreCoords, centreCoords);
    float ripple = dotField * uColor.y * PI - uProgress * uColor.z;

    if (ripple < 0 && ripple > uColor.x * -2 * PI)
    {
        ripple = saturate(sin(ripple));
    }
    else
    {
        ripple = 0;
    }

    float2 sampleCoords = coords + ((ripple * uOpacity / uScreenResolution) * centreCoords);

    return tex2D(uImage0, sampleCoords);
}


technique TriadShockwave
{
    pass TriadShockwavePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}