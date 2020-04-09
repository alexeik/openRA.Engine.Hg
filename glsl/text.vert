#version 430

uniform vec3 Scroll;
uniform vec3 r1, r2;

layout (location = 0 ) in vec3 aVertexPosition;
layout (location = 1 ) in vec2 aTexCoord;
layout (location = 2 ) in vec2 aTexCoordSecond;
layout (location = 3 ) in vec4 aVertexTexMetadata;
layout (location = 4 ) in vec4 aVertexColorInfo;



out vec2 vTexCoord;
out vec4 vTexMetadata;
out vec4 vChannelMask;
out vec4 vDepthMask;
out vec2 vTexSampler;

out vec4 vColorInfo;
out vec4 vColorFraction;
out vec4 vRGBAFraction;
out vec4 vPalettedFraction;
out vec4 vTextColor;

void main()
{

	gl_Position = vec4((aVertexPosition.xyz - Scroll.xyz) * r1 + r2, 1);
	vTexCoord = aTexCoord;
	vTexMetadata = aVertexTexMetadata;

	vColorInfo = aVertexColorInfo;


	vChannelMask = vec4(aVertexColorInfo.s,0,0,0); 

	vTexSampler = vec2(aVertexColorInfo.t,0);
} 
