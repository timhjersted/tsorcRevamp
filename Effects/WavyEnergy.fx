sampler uImage0 : register(s0);

float2 textureToSizeRatio;
float2 effectSize;
float2 textureSize;
float scale;
float time; //Causes the flames to flow with time
float splitAngle; //How wide (in radians) the angle of fire is
float rotation; //Rotates the fire
float length; //The maximum length
float opacity; //Multiplies the output by this to let it fade in
float4 shaderColor;

//I precomputed what values I could, to save on instruction count
float rotationMinusPI;
float splitAnglePlusRotationMinusPI;
float RotationMinus2PIMinusSplitAngleMinusPI;

//Various constants
const float PI = 3.141592;
const float TWOPI = 6.283184;
const float INVERSETWOPI = 0.1591549;
const float4 empty = float4(0, 0, 0, 0);

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 adjCoords = coords * textureToSizeRatio;
    
    //Convert uv from rectangular to polar coordinates
    float2 dir = adjCoords - float2(0.5, 0.5);
    float angle = atan2(dir.y, dir.x);
        
    //Calculate how close the current pixel is to the center line of the screen
    float dist = distance(adjCoords, float2(0.5, 0.5));
    
    //Convert uv from rectangular to polar coordinates
    float2 samplePoint = float2(dist - time, angle * INVERSETWOPI);
    
    //Calculate how intense a pixel should be based on the noise generator
    float intensity = tex2D(uImage0, samplePoint).r;
    
    //Only draw a slice with angles between 'rotation' and 'splitAngle'. The final && is to stop it from going doing a fucky wucky when it crosses the 2Pi > 0 border.    
    if (angle > splitAnglePlusRotationMinusPI)
    {
        return empty;
    }
    if (angle < rotationMinusPI && angle > RotationMinus2PIMinusSplitAngleMinusPI)
    {
        return empty;
    }
    
    //Make it taper off on the edges    
    float edgeDistance = min(min(abs(angle - rotationMinusPI), abs(angle - rotationMinusPI + TWOPI)), min(abs(splitAnglePlusRotationMinusPI - angle), abs(angle - RotationMinus2PIMinusSplitAngleMinusPI)));
    if (abs(edgeDistance) < 0.311)
    {
        intensity = lerp(0.0, intensity, edgeDistance / 0.311);
    }
    
    //Make it taper off near the source
    if (dist < 0.02)
    {
        intensity = lerp(0.0, intensity, dist / 0.02);
    }
    
    if (dist > length)
    {
        intensity = lerp(intensity, 0, min((dist - length) * 10, 1));
    }
    return intensity * float4(0, 0.3, 0.9, 1);

    //Scale 'intensity'
    return intensity * shaderColor * opacity;
}

technique SimpleRing
{
    pass SimpleRingPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}