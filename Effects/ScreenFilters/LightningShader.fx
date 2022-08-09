sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float2 modCoords;
    modCoords.x = coords.x - 0.5;
    float frameX = (coords.x * uImageSize0.x - uSourceRect.x) / uSourceRect.z;
    float frameY = (coords.y * uImageSize0.y - uSourceRect.y) / uSourceRect.w;
    modCoords.y = frameY - 0.5;

    float2 world;

    world.x = 4506.0 * 16;
    world.y = 814.0 * 16;

    float2 imageSize;
    imageSize.x = frameX;
    imageSize.y = frameY;

    float2 pixel = (coords * imageSize) + uTargetPosition;

    float2 diff = world - pixel;


    diff = uTargetPosition - coords;

    float time = (sin(uTime) + 1) / 2;



    if ((modCoords.x * modCoords.x + modCoords.y * modCoords.y) > uSaturation) {
        //color = float4(0, 0, 0, 0);
    }

    if ((diff.x * diff.x + diff.y * diff.y) > uSaturation * 3) {
        color = float4(0, 0, 0, 0);
    }
    return color * sampleColor;
}

technique LightningShader
{
    pass LightningShaderPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}