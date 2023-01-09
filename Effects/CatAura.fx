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
    
    //Compress it so it doesn't bump into the edges of the drawing area
    float progress = ringProgress * 0.7;
    
    //Calculate how close the current pixel is to the center line of the screen
    float distanceIntensity = distance(uv, float2(0.5, 0.5)) * 2;
    
    //Calculate how close the current pixel is from a ring of radius 'ringProgress'
    float ringDistance = 1 - abs(progress - distanceIntensity);
    
    //Check whether the pixel is inside or outside the ring
    if (distanceIntensity < progress)
    {
        //If inside, fade out quickly with distance
        ringDistance = pow(ringDistance, 16);
    }
    else
    {
        //If outside, trail off slower
        ringDistance = pow(ringDistance, 6);
    }
    
    //Convert uv from rectangular to polar coordinates
    float2 dir = uv - float2(0.5, 0.5);
    float angle = atan2(dir.y, dir.x) / (3.141592 * 2);
    float2 samplePoint = float2(distanceIntensity, angle);
    
    //Stretch it so it looks good
    samplePoint = samplePoint * scale * 50 / effectSize;
    samplePoint.y = samplePoint.y * 3;
    
    //Offset it based on time to create the flowing effect
    samplePoint.x = samplePoint.x - (time * 0.05);

    //Get the noise texture at that point
    float sampleIntensity = tex2D(noiseTexture, samplePoint).r;
    
    //Intensify it
    sampleIntensity = pow(sampleIntensity, 2);
    
    //Mix it all together and output it
    return effectColor * fade * sampleIntensity * 2.75 * ringDistance / distanceIntensity;
}


technique CatAura
{
    pass CatAuraPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}