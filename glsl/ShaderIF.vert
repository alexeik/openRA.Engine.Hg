#version 430

layout (location = 0 )  in vec3 vVertexPosition;
layout (location = 1 )  in float vShaderID;
layout (location = 2 )  in float vCurrentFrame;
layout (location = 3 )  in float vTotalFrames;
layout (location = 4 )  in float viTime;
layout (location = 5 )  in float vTotalTime;
layout (location = 6 )  in vec2 viResolutionXY;
layout (location = 7 )  in float vTextureInputSlot;
layout (location = 8 )  in float vTextureStoreChannel;
layout (location = 9 )  in vec2 vSpriteUVCoords;
layout (location = 10 )  in float vPaletteIndex;
layout (location = 11 ) in float temp;

uniform vec3 Scroll;
uniform vec3 r1, r2;

// vertexbuffer format mapping



out float ShaderID;
out float CurrentFrame;
out float TotalFrames;
out float iTime;
out float TotalTime;
 
out vec2 iResolutionXY;
out float TextureInputSlot;
out float TextureStoreChannel;
out vec2 SpriteUVCoords;
out float PaletteIndex;
out vec2 fragXY;

vec4 SelectStoreChannel(float x)
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

void main()
{
 	gl_Position = vec4((vVertexPosition.xyz - Scroll.xyz) * r1 + r2, 1);
	if (gl_VertexID==0)
	{
	fragXY=vec2(0,0);
	}
	if (gl_VertexID==1)
	{
	fragXY=vec2(1,0);
	}
	if (gl_VertexID==2)
	{
	fragXY=vec2(0,1);
	}
	if (gl_VertexID==3)
	{
	fragXY=vec2(1,1);
	}
	ShaderID=vShaderID;
	CurrentFrame=vCurrentFrame;
	TotalFrames=vTotalFrames;
	iTime=viTime;
	TotalTime=vTotalTime;
	iResolutionXY=viResolutionXY;
	TextureInputSlot=vTextureInputSlot;
	TextureStoreChannel=vTextureStoreChannel;
	SpriteUVCoords=vSpriteUVCoords;
	PaletteIndex=vPaletteIndex;

} 
