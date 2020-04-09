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
//роутер для выбора текстуры. он приходит из UnpackChannelAttributes, если в вертексбуфере, в
// в последнем столбце например 65, то это возьмет primarySampler (3 координату из UnpackChannelAttributes) 
// приведет к vTexSampler.s=1 и выберет Texture1 в атрибутах шейдера

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
/*vTexMetadata.t convert to vTexMetadata.p because vTexMetadata = 4 pcs of float*/
void main()
{
	vec4 c ;
		
	if (DrawMode==0.0) // рисует пиксели из RGBA текстуры
	{
	 vec4 x = Sample(vTexSampler.t, vTexCoord.st); //возвращает структуру (R,G,B,A) из текстуры
	//vec4 c = vRGBAFraction * x ;
	 c =  x ; // vRGBAFraction всегда 1,1,1,1

	}
	if (DrawMode==6.0) // рисует пиксели из RGBA текстуры заполняя область
	{
	 vec2 spriteRange = (EndUV - StartUV );
	
	 vec2 uv = StartUV + fract(vTexCoord.st) * spriteRange;
	 
	 vec4 x = Sample(vTexSampler.t,uv);

	 c =  x ; 

	}
	if (DrawMode==7.0) // рисует пиксели из 1 канальной текстуры заполняя область
	{
		
	 vec2 spriteRange = (EndUV - StartUV );

	 vec2 uv =StartUV + fract(vTexCoord) * spriteRange;

	 /* uv.x = StartUV.x + fract(vTexCoord.x) * spriteRange.x;
	 uv.y = StartUV.y + fract(vTexCoord.y) * spriteRange.y; */
	 //алгоритм расжатия или демасштабирования. его нужно отключить,
	 //если у нас 1 к 1 размер спрайта и полигона
	 
	  vec4 x;
	 //c=vec4(floor(vTexCoord),fract(vTexCoord));
/* 	  if (hk<=1.0f && wk<=1.0f)
	 {
		x = Sample(vTexSampler.t, vTexCoord.st); //забираем 4 байта из текстуры
	 }
	 else
	 {
	  x = Sample(vTexSampler.t, uv); //забираем 4 байта из текстуры
	 }  */
	 x = Sample(vTexSampler.t, uv); //забираем 4 байта из текстуры
	 vec2 p = vec2(dot(x, vChannelMask), PaletteIndex);   // определяем байт в котором указатель на цвет, через vChannelMask - укажет единичкой, какой байт использовать)
	
	
	 c =  texture2D(Palette, p) ;//запрос цвета в палитер; 


	 
	}
	if (DrawMode==1.0) // рисует пиксели из палитры
	{
		vec4 x = Sample(vTexSampler.t, vTexCoord.st);
		
		//vTexMetadata.s вертикальный индекс палитры содержит. 
		vec2 p = vec2(dot(x, vChannelMask), PaletteIndex);  
		
		//vec2 p = vec2(dot(x, vec4(1,0,0,0)), vTexMetadata.s); //статичное определение маски, всегда в R канале
		// c = vec4(1,1,1,1) * texture2D(Palette, p) ;
		c = vPalettedFraction * texture2D(Palette, p) ;//запрос цвета в палитер
	}
	if (DrawMode==2.0) //UI
	{
		//vec4 c = vColorFraction * vTexCoord;
		 c =  vec4(vTexCoord.st,vTexCoordSecond.st); // vColorFraction всегда 1,1,1,1

	}
	// 3.0 зарезервировано под MSDF в text.frag
	if (DrawMode==4.0)
	{
		//IMGUI внутренняя ветка.
		//vec4 c = vColorFraction * vTexCoord; 
		 vec4 x = Sample(PaletteIndex, vTexCoord.st);
		//Texture0 текстура шрифта от ImGui , из нее берет цвета для себя.
		 c =  vec4(vColorInfo) * x ;//  texture2D(Texture0,vTexCoord.st); // vColorFraction всегда 1,1,1,1
	}
	if (DrawMode==5.0)
	{
		//IMGUI для спрайтов из игры - ветка.
		//vec4 c = vColorFraction * vTexCoord; 
		 vec4 x = Sample(vTexSampler.t, vTexCoord.st);
		 //vec4 x = texture2D(Texture1,vTexCoord.st); // vColorFraction всегда 1,1,1,1
		 vec2 p = vec2(dot(x, vChannelMask), PaletteIndex);
		 c = vec4(1,1,1,1) * texture2D(Palette, p) ;
	}
	if (c.a == 0.0)
		discard;

	float depth = gl_FragCoord.z;
	//используется для дебаг режима
	if (length(vDepthMask) > 0.0)
	{
		vec4 y = Sample(vTexSampler.t, vTexCoordSecond.st);
		depth = depth + DepthTextureScale * dot(y, vDepthMask);
	}

	// Convert to window coords
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
