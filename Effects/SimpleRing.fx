sampler uImage0 : register(s0);

float2 textureToSizeRatio;
float2 effectSize;
float2 textureSize;
float splitAngle; //How wide (in radians) the angle of fire is
float rotation; //Rotates the fire
float length; //The maximum length
float4 shaderColor;
float firstEdge;
float secondEdge;

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
    
    //Calculate how intense a pixel should be based on the noise generator
   // float intensity = tex2D(uImage0, samplePoint).r;
    
    float intensity = 1;
    
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
    if (abs(edgeDistance) < firstEdge)
    {
        intensity = lerp(0.0, intensity, edgeDistance / firstEdge);
    }
    
    //Make it taper off near the source
    if (dist < secondEdge)
    {
        intensity = lerp(0.0, intensity, dist / secondEdge);
    }
    
    intensity = lerp(intensity, 0, pow(min(abs(dist - length) * 10, 1), .4));
    //return intensity * float4(1,1,1,1);
    
    return intensity * shaderColor;
}

technique SimpleRing
{
    pass SimpleRingPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}