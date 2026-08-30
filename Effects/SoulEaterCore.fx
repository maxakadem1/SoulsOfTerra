sampler bodyTexture : register(s0);

float coreTime;
float coreIntensity;
float2 coreTextureSize;

float CoreMask(float2 uv)
{
	float4 sampleColor = tex2D(bodyTexture, uv);
	float cyanChroma = min(sampleColor.g, sampleColor.b) - sampleColor.r * 0.72;
	float brightness = (sampleColor.g + sampleColor.b) * 0.5;
	return sampleColor.a * smoothstep(0.06, 0.28, cyanChroma) * smoothstep(0.12, 0.58, brightness);
}

float4 SoulEaterCorePixel(float2 uv : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
	float2 texel = 1.5 / coreTextureSize;
	float centerMask = CoreMask(uv);
	float neighboringMask = 0.0;
	neighboringMask = max(neighboringMask, CoreMask(uv + float2(texel.x, 0.0)));
	neighboringMask = max(neighboringMask, CoreMask(uv - float2(texel.x, 0.0)));
	neighboringMask = max(neighboringMask, CoreMask(uv + float2(0.0, texel.y)));
	neighboringMask = max(neighboringMask, CoreMask(uv - float2(0.0, texel.y)));
	neighboringMask = max(neighboringMask, CoreMask(uv + texel));
	neighboringMask = max(neighboringMask, CoreMask(uv - texel));
	neighboringMask = max(neighboringMask, CoreMask(uv + float2(texel.x, -texel.y)));
	neighboringMask = max(neighboringMask, CoreMask(uv + float2(-texel.x, texel.y)));

	float edge = saturate(neighboringMask - centerMask * 0.38);
	float2 pixelUv = floor(uv * coreTextureSize * 0.5) * 2.0 / coreTextureSize;
	float slowFlow = sin(pixelUv.y * 38.0 - coreTime * 1.7 + sin(pixelUv.x * 31.0 + coreTime * 0.8) * 1.4);
	float crossFlow = sin((pixelUv.x + pixelUv.y) * 47.0 + coreTime * 1.15);
	float flow = saturate(0.56 + slowFlow * 0.24 + crossFlow * 0.14);

	float interior = centerMask * (0.58 + flow * 0.52);
	float alpha = saturate(interior + edge * (0.55 + flow * 0.3));
	alpha *= coreIntensity * vertexColor.a;

	float3 deepSoul = float3(0.015, 0.28, 0.27);
	float3 spectralCyan = float3(0.08, 0.88, 0.76);
	float3 paleCore = float3(0.68, 1.0, 0.91);
	float3 color = lerp(deepSoul, spectralCyan, flow);
	color = lerp(color, paleCore, saturate(interior * 0.62 + edge * 0.38));

	// Additive SpriteBatch expects intensity in both color and alpha.
	return float4(color * alpha, alpha);
}

technique SoulEaterCore
{
	pass CorePass
	{
		PixelShader = compile ps_3_0 SoulEaterCorePixel();
	}
};
