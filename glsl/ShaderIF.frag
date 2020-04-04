#version 430

out vec4 fragColor;

uniform sampler2D Texture0;
uniform sampler2D Texture1;
uniform sampler2D Texture2;
uniform sampler2D Texture3;
uniform sampler2D Texture4;
uniform sampler2D Texture5;
uniform sampler2D Texture6;
uniform sampler2D Palette;
uniform sampler2DArray TextureFontMSDF;

uniform bool EnableDepthPreview;
uniform float DepthTextureScale;
uniform vec3 TextColor;

in float ShaderID;
in float CurrentFrame;
in float TotalFrames;
in float iTime;
in float TotalTime;
 
in vec2 iResolutionXY;
in float TextureInputSlot;
in vec4 TextureStoreChannel;
in vec2 SpriteUVCoords;
in float PaletteIndex;
in vec2 fragXY;

vec4 Sample(float samplerIndex, vec2 pos)
{
	
	if (samplerIndex ==1)
		return texture2D(Texture0, pos);
	else if (samplerIndex < 1.5)
		return texture2D(Texture1, pos);
	else if (samplerIndex < 2.5)
		return texture2D(Texture2, pos);
	else if (samplerIndex < 3.5)
		return texture2D(Texture3, pos);
	else if (samplerIndex < 4.5)
		return texture2D(Texture4, pos);
	else if (samplerIndex < 5.5)
		return texture2D(Texture5, pos);

	return texture2D(Texture6, pos);
}



void main()
{
	
	if (ShaderID==1)
	{
		
		vec4 pixel_palette_shortcut=Sample(TextureInputSlot,SpriteUVCoords);
		vec4 pixel_from_palette=texture(Palette,vec2(dot(pixel_palette_shortcut,TextureStoreChannel),PaletteIndex));
		//pixel_palette_shortcut.r компоненту нужно регулировать, так как пиксели храняться
		//в разных каналах r g b a.
		vec2 uv = fragXY;
		
		uv -=0.5; //center circle origin
		uv.x *= iResolutionXY.x/iResolutionXY.y; //ratio width/height
		
		float le=length(uv);
		float r=0.2+CurrentFrame*.01;
		vec3 c;
		if (le<r) c=pixel_from_palette.rgb; else c=vec3(0.);
									 
		fragColor = vec4(c,1);

	}
	
	
}
