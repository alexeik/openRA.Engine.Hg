#version 430

uniform vec3 Scroll;
uniform vec3 r1, r2;

layout (location = 0 ) in vec3 aVertexPosition;
layout (location = 1 ) in vec2 aTexCoord;
layout (location = 2 ) in vec2 aTexCoordSecond;

layout (location = 3 ) in float aVertexPaletteIndex;
layout (location = 4 ) in float aVertexTexMetadataOption;
layout (location = 5 ) in float aVertexDrawmode;
layout (location = 6 ) in float aVertexTexMetadataOption2;

layout (location = 7 ) in vec4 aVertexColorInfo;
layout (location = 8 ) in vec4 aVertexUVFillRect;

out vec2 vTexCoord;
out vec2 vTexCoordSecond;

out vec4 vChannelMask;
out vec4 vDepthMask;
out vec2 vTexSampler;

out vec4 vColorInfo;
out vec4 vColorFraction;
out vec4 vRGBAFraction;
out vec4 vPalettedFraction;
out vec2 fragXY;
out vec2 StartUV;
out vec2 EndUV;
out float DrawMode;
out float PaletteIndex;

/* 


vec4 SelectColorFraction(float x)
{
	if (x > 0.0)
		return vec4(0, 0, 0, 0);

	return vec4(1, 1, 1, 1);
}

vec4 SelectRGBAFraction(float x)
{
	if (x == 2.0)
		return vec4(1, 1, 1, 1);

	return vec4(0, 0, 0, 0);
}


 */
 
 
vec4 SelectChannelMask(float x)
{
	if (x == 4.0)
		return vec4(1,1,1,1);
	if (x == 3.0)
		return vec4(0,0,0,1);
	if (x == 2.0)
		return vec4(0,0,1,0);
	if (x == 1.0)
		return vec4(0,1,0,0);

	if (x == 0.0)
		return vec4(1,0,0,0);

	return vec4(0, 0, 0, 0);
}
vec4 SelectPalettedFraction(float x)
{
	
	return vec4(1, 1, 1, 1);
	if (x == 0.0 || x == 2.0)
		return vec4(0, 0, 0, 0);

	
}
void main()
{
 
	gl_Position = vec4((aVertexPosition.xyz - Scroll.xyz) * r1 + r2, 1);
	
	vTexCoordSecond=aTexCoordSecond;
	vTexCoord=aTexCoord;
	DrawMode=aVertexDrawmode;
	PaletteIndex=aVertexPaletteIndex;

	vColorInfo = aVertexColorInfo;

	vPalettedFraction = SelectPalettedFraction(aVertexColorInfo.s);
	vChannelMask = SelectChannelMask(aVertexColorInfo.s);

	
	vTexSampler = vec2(0,aVertexColorInfo.t);

  	StartUV = aVertexUVFillRect.rg;
	EndUV = aVertexUVFillRect.ba; 



} 
