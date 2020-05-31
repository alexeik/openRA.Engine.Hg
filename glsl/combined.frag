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

uniform sampler2DArray Texture2D0;
uniform sampler2DArray Texture2D1;
uniform sampler2DArray Texture2D2;

uniform bool EnableDepthPreview;
uniform float DepthTextureScale;
uniform vec2 MouseLocation;
uniform vec3 AlphaConstantRegion;
uniform vec3 AlphaInit;
uniform bool AlphaFlag;
uniform bool FrameBufferMaskMode;

uniform vec4 Layer1KeyColors[10];
uniform vec4 Layer2KeyColors[10];
uniform vec4 Layer3KeyColors[10];
uniform vec4 Layer4PickKeyColors[10]; //HighLight pick regions colors

uniform float iTime;

uniform vec4 Layer1Color[1];
uniform vec4 Layer2Color[1];
uniform vec4 Layer3Color[1];

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
in float VertexTexMetadataOption2;
in float TextureArrayIndex;
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
		vec4 x;
		if (VertexTexMetadataOption2==0)
		{
			x = Sample(vTexSampler.t, vTexCoord.st); //пишет в bgra порядке в текстуру
		}
		else
		{
			x = texture(Texture2D0,vec3(vTexCoord.st,TextureArrayIndex));
		}
		
	 //vec4 x = Sample(vTexSampler.t, vTexCoord.st); //возвращает структуру (R,G,B,A) из текстуры
	//vec4 c = vRGBAFraction * x ;
		c =  x ; // vRGBAFraction всегда 1,1,1,1

	}
	
	if (DrawMode==1.0) // рисует пиксели из палитры
	{
		vec4 x;
		vec2 p;
		if (VertexTexMetadataOption2==0)
		{
			x = Sample(vTexSampler.t, vTexCoord.st);
			p = vec2(dot(x, vChannelMask), PaletteIndex);  
			
		}
		else
		{

			x = texture(Texture2D0,vec3(vTexCoord.st,TextureArrayIndex));
			p = vec2(x.r, PaletteIndex);  
		}
		//vec4 x = Sample(vTexSampler.t, vTexCoord.st);
		
		//vTexMetadata.s вертикальный индекс палитры содержит. 
		//vec2 p = vec2(dot(x, vChannelMask), PaletteIndex);  
		
		//vec2 p = vec2(dot(x, vec4(1,0,0,0)), vTexMetadata.s); //статичное определение маски, всегда в R канале
		// c = vec4(1,1,1,1) * texture2D(Palette, p) ;
		c = vPalettedFraction * texture2D(Palette, p) ;//запрос цвета в палитре
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
		 vec2 p = vec2(x.r, PaletteIndex);
		 c = vec4(1,1,1,1) * texture2D(Palette, p) ;
	}
	
	if (DrawMode==6.0) // рисует пиксели из RGBA текстуры заполняя область
	{
		 vec2 spriteRange = (EndUV - StartUV );
		
		 vec2 uv = StartUV + fract(vTexCoord.st) * spriteRange;
		 vec4 x;
		vec2 p;
		 //vec4 x = Sample(vTexSampler.t,uv);
		 if (VertexTexMetadataOption2==0) //disabled with==3
		{
			x = Sample(vTexSampler.t, vTexCoord.st);
		
			
		}
		else
		{
			
			x = texture(Texture2D0,vec3(uv,TextureArrayIndex)); //переставляет тут, так как в FastCopyIntoChannel r=b , b=r
		}
		 c =  x ; 

	}
	if (DrawMode==7.0) // рисует пиксели из 1 канальной текстуры заполняя область
	{
			
		 vec2 spriteRange = (EndUV - StartUV );

		 vec2 uv =StartUV + fract(vTexCoord) * spriteRange;

		
		  vec4 x;

		 //x = Sample(vTexSampler.t, uv); //забираем 4 байта из текстуры
		 x=texture(Texture2D0,vec3(uv,TextureArrayIndex));
		 vec2 p = vec2(x.r, PaletteIndex);   // определяем байт в котором указатель на цвет, через vChannelMask - укажет единичкой, какой байт использовать)
		
		
		 c =  texture2D(Palette, p) ;//запрос цвета в палитер; 
	}
	if (DrawMode==8.0) // рисует спрайты тайлов карты из мегатекстуры.
	{
		vec4 x;
		vec2 tilexy=floor(vTexCoord);
		 vec2 uv = fract(vTexCoord);
		x = Sample(vTexSampler.t, vTexCoord.st); //забираем 4 байта из текстуры
		if (x.r>=0.01961 && x.g>=0.01961 && x.b>=0.01961)
		{
			x=vec4(0.65234,0.30469,0.09082,1);
		}
			//x=vec4(1,0.82745,0.4902,1);
			c=x;
	}
	
		
	if (DrawMode==9.0) //dump texture to png
	{
		 c=texture(Texture2D0,vec3(vTexCoord,TextureArrayIndex));
		//c=texture2D(Texture0, vTexCoord);
		//c=vec4(1,1,1,1);
	}	
	
	if (DrawMode==10.0) //для текстуры+маски алгоритм для карты
	{
			vec4 usercolor;
			if (MouseLocation.x!=-1)
			{
				//Texture0 хранит текстуру карту масок
				//Texture1 хранит текстуру фреймбуфера - то , что видит игрок
				
				vec4 highlightcolor = texture(Texture0,vec2(MouseLocation));//перевернули уже в openra по вертикали
				vec4 highlightcolor2 = texture(Texture0,vec2(vTexCoord.s,1-vTexCoord.t));
				vec4 excludecolor=vec4(0,0,0,0);
				
				usercolor= texture(Texture1,vec2(vTexCoord.s,1-vTexCoord.t));
				
				
				//блок отключен, так как обработка идет послойно в одном уровне кода
				
				
					c=usercolor ;// texture(Texture1,vec2(vTexCoord.s,1-vTexCoord.t));
					for(int i=0;i<10;++i)
					{
						
						//free layers for map. because 3 fraction
						if (Layer1KeyColors[i]==vec4(0,0,0,0))
							continue;
						if (highlightcolor2==Layer1KeyColors[i])
						{
							//c = Layer1Color[0];
							c= mix(usercolor, Layer1Color[0], 0.8);
						}
						if (Layer2KeyColors[i]==vec4(0,0,0,0))
							continue;
						if (highlightcolor2==Layer2KeyColors[i])
						{
							//c = Layer2Color[0];
							c= mix(usercolor, Layer2Color[0], 0.8);
						}
						if (Layer3KeyColors[i]==vec4(0,0,0,0))
							continue;
						if (highlightcolor2==Layer3KeyColors[i])
						{
							//c = Layer3Color[0];
							c= mix(usercolor, Layer3Color[0], 0.8);
						}
						
						
						
					}
					
					for(int i=0;i<10;++i)
					{
					//pickuplayer
						if (Layer4PickKeyColors[i]==vec4(0,0,0,0))
							continue;
						if (highlightcolor2==Layer4PickKeyColors[i])
						{
							//c = Layer1Color[0];
							c= mix(usercolor, vec4(iTime/255,iTime/255,1,1), 0.8);
						}
					}
					if (highlightcolor2==highlightcolor && highlightcolor!=excludecolor )
					{
					//подсветку , только PickRegions
						if (FrameBufferMaskMode) //для опроса цвета в нажатом пикселе
						{
							c=highlightcolor;
						}
						else
						{
							for(int i=0;i<10;++i)
							{
							//pickuplayer
								 if (Layer4PickKeyColors[i]==vec4(0,0,0,0))
								{
									//c= vec4(0.6,0,0,1);
									//continue;
								}
								if (highlightcolor2==Layer4PickKeyColors[i])
								{
									//c= mix(usercolor, vec4(0.6,0,0,1), 0.7); // для режима выбора областей наступления
									c= vec4(1,1,1,1); // для режима выбора областей наступления
								}
								else
								{
									//c=usercolor;
								}
							}
						}
					}
					
					
				
				//переделать черный из прозрачного в черный.
				/* if (c==vec4(0,0,0,0))
				{
					c=vec4(0,0,0,1);
				} */
				
				
			}


	}
	if (DrawMode==11.0) //для текстуры+маски алгоритм для домов
	{
			vec4 hlcolor;
			vec4 highlightcolor;
			vec4 highlightcolor2;
			
			if (MouseLocation.x!=-1)
			{
				//Texture1 хранит текстуру фреймбуфера
				//Texture0 хранит текстуру карту масок
				 if (VertexTexMetadataOption2==1) //disabled with==3
					{
						
					}
				else
				{
					highlightcolor = texture(Texture0,vec2(MouseLocation));//перевернули уже в openra по вертикали
					highlightcolor2 = texture(Texture0,vec2(vTexCoord.s,1-vTexCoord.t));
					
					
					hlcolor= texture(Texture1,vec2(vTexCoord.s,1-vTexCoord.t));
				
				}
			
				
				
				if (highlightcolor2==highlightcolor && highlightcolor!=vec4(AlphaConstantRegion,1))
				{


						if (FrameBufferMaskMode)
						{
							c=highlightcolor;
						}
						else
						{
							c= vec4(hlcolor.rgb, 1); //для режима выбора домов
						}

				
				}
				else
				{
		
						c=hlcolor;
					
		
						
						if (highlightcolor2==vec4(AlphaConstantRegion,1))
						{
							
						}
						else
						{
							c=vec4(c.r,c.g,c.b,0.5); //AlphaInit.r содержит значение для альфы.
							
							//c=vec4(c.r,c.g,c.b,0); //AlphaInit.r содержит значение для альфы.
						}

				}

				
				
			}
			else
			{
				c= texture(Texture1,vec2(vTexCoord.s,1-vTexCoord.t));
				//c = texture(Texture2D0,vec3(vTexCoord.st,TextureArrayIndex));
			
			
			//c=hlcolor;
			
			} 
			

	}
	
	if (DrawMode==12.0) //для карты, выбор среди заданных областей
	{
		vec4 usercolor = texture(Texture0,vec2(vTexCoord.s,1-vTexCoord.t));
		vec4 maskcolor = texture(Texture1,vec2(vTexCoord.s,1-vTexCoord.t));
		c=usercolor;
		if (maskcolor==vec4(0.66666669,0,0.66666669,1))
		{
			c=vec4(0,0,0,1);
		}
		//accept Texture0 for original
		//accept PatchLookupColor for color lookup
		//accept PatchReplaceColor for color replace
		
		//accept PatchLookupColor for color lookup по-этой площади будут заменяться пиксели из Texture0
		//accept PatchReplaceColor for color replace по-этой площади будут браться пиксели из PatchTextureSlot
		//accept PatchTextureSlot for pixel replace
		
		//Patch texture must be same size as original.
		
		//patch texture 
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
