sampler noiseTexture : register(s0);
float textureSize;
float2 effectSize;
float4 effectColor;
float time;
float ringProgress;
float fadePercent;
float scaleFactor;


float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    //Set scale
    float scale = 1;
    if (scaleFactor != 0)
    {
        scale = scaleFactor;
    }
    
    //Invert this so that 'full' is the default
    float fade = 1 - fadePercent;
    
    //Normalize pixel coordinates (from 0 to 1) and compensate for effect size distortion
    float2 uv = coords * textureSize;
    uv.x = uv.x / effectSize.x;
    uv.y = uv.y / effectSize.y;
    
    float2 centeredCoords = uv - float2(0.5, 0.5);
    centeredCoords *= 5;
    
    float intensity = pow(centeredCoords.x, 2.0 / 3.0) + pow(centeredCoords.y, 2.0 / 3.0);
    intensity = 1 / pow(intensity, 1.5);
    //return effectColor * intensity;
    
    //Compress it so it doesn't bump into the edges of the drawing area
    
    //Calculate how close the current pixel is to the center line of the screen
    float distanceIntensity = distance(uv, float2(0.5, 0.5)) * 2;
    
    
    //Convert uv from rectangular to polar coordinates
    float2 dir = uv - float2(0.5, 0.5);
    float angle = atan2(dir.y, dir.x) / (3.141592 * 2);
    float2 samplePoint = float2(intensity, angle);
    
    //Stretch it so it looks good
    samplePoint = samplePoint * scale * (50 / 3) / effectSize;
    samplePoint.y = samplePoint.y * 1;
    
    //Offset it based on time to create the flowing effect
    samplePoint.x = samplePoint.x - (time * 0.05);

    //Get the noise texture at that point
    float sampleIntensity = tex2D(noiseTexture, samplePoint).r;
    
    return sampleIntensity * intensity * intensity * effectColor * 2 * fade;
}


technique CatFinalStandAttack
{
    pass CatFinalStandAttack
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}