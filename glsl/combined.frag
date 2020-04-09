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

in vec2 vTexCoord;
in vec2 vTexCoordSecond;


in vec4 vChannelMask;
in vec4 vDepthMask;
in vec2 vTexSampler;

in vec4 vColorInfo;
in vec4 vColorFraction;
in vec4 vRGBAFraction;
in vec4 vPalettedFraction;
in vec2 fragXY;
in vec2 StartUV;
in vec2 EndUV;
in float DrawMode;
in float PaletteIndex;


float jet_r(float x)
{
	return x < 0.7 ? 4.0 * x - 1.5 : -4.0 * x + 4.5;
}

float jet_g(float x)
{
	return x < 0.5 ? 4.0 * x - 0.5 : -4.0 * x + 3.5;
}

float jet_b(float x)
{
	return x < 0.3 ? 4.0 * x + 0.5 : -4.0 * x + 2.5;
}


vec4 Sample(float samplerIndex, vec2 pos)
{
	if (samplerIndex < 0.5)
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
	vec4 c ;
		
	if (DrawMode==0.0) 
	{
	 vec4 x = Sample(vTexSampler.t, vTexCoord.st); 

	 c =  x ; 

	}
	if (DrawMode==6.0)
	{
	 vec2 spriteRange = (EndUV - StartUV );
	
	 vec2 uv = StartUV + fract(vTexCoord.st) * spriteRange;
	 
	 vec4 x = Sample(vTexSampler.t,uv);

	 c =  x ; 

	}
	if (DrawMode==7.0)
	{
		
	 vec2 spriteRange = (EndUV - StartUV );

	 vec2 uv =StartUV + fract(vTexCoord) * spriteRange;

	 
	  vec4 x;

	 x = Sample(vTexSampler.t, uv);
	 vec2 p = vec2(dot(x, vChannelMask), PaletteIndex);  
	
	
	 c =  texture2D(Palette, p) ;


	 
	}
	if (DrawMode==1.0) 
	{
		vec4 x = Sample(vTexSampler.t, vTexCoord.st);
		
	
		vec2 p = vec2(dot(x, vChannelMask), PaletteIndex);  
		
		
		c = vPalettedFraction * texture2D(Palette, p) 
	}
	if (DrawMode==2.0) //UI
	{

		 c =  vec4(vTexCoord.st,vTexCoordSecond.st); 

	}

	if (DrawMode==4.0)
	{
	
		 vec4 x = Sample(PaletteIndex, vTexCoord.st);
	
		 c =  vec4(vColorInfo) * x ;
	}
	if (DrawMode==5.0)
	{
		
		 vec4 x = Sample(vTexSampler.t, vTexCoord.st);

		 vec2 p = vec2(dot(x, vChannelMask), PaletteIndex);
		 c = vec4(1,1,1,1) * texture2D(Palette, p) ;
	}
	if (c.a == 0.0)
		discard;

	float depth = gl_FragCoord.z;

	if (length(vDepthMask) > 0.0)
	{
		vec4 y = Sample(vTexSampler.t, vTexCoordSecond.st);
		depth = depth + DepthTextureScale * dot(y, vDepthMask);
	}


	gl_FragDepth = 0.5 * depth + 0.5;

	if (EnableDepthPreview)
	{
		float x = 1.0 - gl_FragDepth;
		float r = clamp(jet_r(x), 0.0, 1.0);
		float g = clamp(jet_g(x), 0.0, 1.0);
		float b = clamp(jet_b(x), 0.0, 1.0);
		fragColor = vec4(r, g, b, 1.0);
	}
	else
	{
		fragColor = c;
		

	
	}
	
}
