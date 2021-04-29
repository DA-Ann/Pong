#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif


float3 LightPosition = float3(0,20,0);
float4 LightColor = float4(1, 0, 0, 1);


float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldInverseTranspose;
float LightRadius = 1000;

static const float PI = 3.14159265f;

float4 AmbientColor = float4(1, 0, 1, 1);
float AmbientIntensity = 0.75;

float4 CubeColor = float4(1,1,1,1);


struct VertexShaderInput
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL0;
	float4 WorldPosition : POSITIONT;

};


VertexShaderOutput VertexShaderFunction(VertexShaderInput input) {
	VertexShaderOutput output;

	float4 worldPosition = mul(input.Position, World);
	output.WorldPosition = worldPosition;
	float4 viewPosition = mul(worldPosition, View);
	output.Position = mul(viewPosition, Projection);
	float3 normal = mul(input.Normal, (float3x3)WorldInverseTranspose);
	output.Normal = normal;

	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0{
	float4 diffuseColor = 0;
	
	
		float3 lightDirection = LightPosition - (float3)input.WorldPosition;
		float3 normal = normalize(input.Normal);
		float intensity = pow(1 - saturate(length(lightDirection) / LightRadius), 2);
		lightDirection = normalize(lightDirection);
		float3 view = normalize((float3)input.WorldPosition); 
		diffuseColor += dot(normal, lightDirection) * intensity * LightColor;
		
	return saturate(diffuseColor + AmbientColor * AmbientIntensity);
}

technique PointLight
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 VertexShaderFunction();
		PixelShader = compile ps_4_0 PixelShaderFunction();
	}
}