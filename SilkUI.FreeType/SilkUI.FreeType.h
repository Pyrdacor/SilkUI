#pragma once

// The freetype headers use 'generic' as a variable name.
// As 'generic' is a keyword in C++/CLI we need this hack.
#define generic generic_

extern "C"
{
#include <ft2build.h>
#include FT_FREETYPE_H
}

#undef generic

#include <string>

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace System::Text;

namespace SilkUI
{
	namespace FreeType
	{
		public value struct Glyph
		{
			UInt32 CharCode;
			Int32 Width;
			Int32 Height;
			Int32 BearingX;
			Int32 BearingY;
			Int32 Advance;
			array<Byte>^ ImageData;
		};

		public value struct FontFace
		{
			Int32 FaceIndex;
			Boolean Bold;
			Boolean Italic;
			array<Glyph>^ Glyphs;
		};

		public value struct Font
		{
			String^ Family;
			Int32 Size;
			array<FontFace>^ Faces;
		};

		struct FontInfo
		{
			std::string Family;
			int NumFaces;
			int LineHeight;
		};

		private class FreeTypeInterface
		{
		private:
			FT_Library library;

		public:
			FreeTypeInterface()
			{
				if (FT_Init_FreeType(&library) != FT_Err_Ok)
					throw gcnew System::Exception("Error initializing native FreeType. Are you missing the freetype library?");
			}

			~FreeTypeInterface()
			{
				FT_Done_FreeType(library);
			}

			static std::string StringToCpp(String^ str)
			{
				array<unsigned char>^ bytes = Encoding::UTF8->GetBytes(str);
				pin_ptr<unsigned char> pinned = &bytes[0];
				return std::string((char*)pinned, bytes->Length);
			}

			Font LoadFont(String^ fontFile, Int32 fontSize)
			{
				FontInfo fontInfo;
				FontFace firstFace = LoadFontFace(fontFile, fontSize, 0, fontInfo);
				return ProcessFont(firstFace, fontFile, fontSize, fontInfo);
			}

			Font LoadFont(array<Byte>^ data, Int32 fontSize)
			{
				FontInfo fontInfo;
				FontFace firstFace = LoadFontFace(data, fontSize, 0, fontInfo);
				return ProcessFont(firstFace, data, fontSize, fontInfo);
			}

			FontFace LoadFontFace(String^ fontFile, Int32 fontSize, Int32 faceIndex, FontInfo& fontInfo)
			{
				FT_Face face;

				if (FT_New_Face(library, StringToCpp(fontFile).c_str(), faceIndex, &face) != FT_Err_Ok)
					throw gcnew System::Exception("Error loading font '" + fontFile + "'.");

				try
				{
					return ProcessFontFace(face, fontSize, fontInfo);

				}
				finally
				{
					FT_Done_Face(face);
				}
			}

			FontFace LoadFontFace(array<Byte>^ data, Int32 fontSize, Int32 faceIndex, FontInfo& fontInfo)
			{
				FT_Face face;
				pin_ptr<FT_Byte> pinned = &data[0];

				if (FT_New_Memory_Face(library, pinned, data->Length, faceIndex, &face) != FT_Err_Ok)
					throw gcnew System::Exception("Error loading font from memory.");

				try
				{
					return ProcessFontFace(face, fontSize, fontInfo);
				}
				finally
				{
					FT_Done_Face(face);
				}
			}

			Font ProcessFont(FontFace firstFace, String^ fontFile, Int32 fontSize, FontInfo& fontInfo)
			{
				Font font = CreateFont(firstFace, fontSize, fontInfo);

				for (int i = 1; i < fontInfo.NumFaces; ++i)
				{
					font.Faces[i] = LoadFontFace(fontFile, fontSize, i, fontInfo);
				}

				return font;
			}

			Font ProcessFont(FontFace firstFace, array<Byte>^ data, Int32 fontSize, FontInfo& fontInfo)
			{
				Font font = CreateFont(firstFace, fontSize, fontInfo);

				for (int i = 1; i < fontInfo.NumFaces; ++i)
				{
					font.Faces[i] = LoadFontFace(data, fontSize, i, fontInfo);
				}

				return font;
			}

			Font CreateFont(FontFace firstFace, Int32 fontSize, const FontInfo& fontInfo)
			{
				Font font;

				font.Family = gcnew String(fontInfo.Family.c_str());
				font.Size = fontSize;
				font.Faces = gcnew array<FontFace>(fontInfo.NumFaces);
				font.Faces[0] = firstFace;

				return font;
			}

			FontFace ProcessFontFace(FT_Face face, Int32 fontSize, FontInfo& fontInfo)
			{
				FT_Set_Pixel_Sizes(face, 0, fontSize);

				FontFace fontFace;
				FT_ULong charCode;
				FT_UInt glyhpIndex;
				List<Glyph>^ glyphs = gcnew List<Glyph>();

				if (face->face_index == 0)
				{
					// Only fill font info when first face is processed.
					fontInfo.Family = face->family_name;
					fontInfo.NumFaces = face->num_faces;
					fontInfo.LineHeight = face->max_advance_height;

					if (fontInfo.NumFaces < 1)
						fontInfo.NumFaces = 1;
					if (fontInfo.LineHeight < 1)
						fontInfo.LineHeight = fontSize;
				}

				charCode = FT_Get_First_Char(face, &glyhpIndex);

				while (glyhpIndex != 0)
				{
					if (FT_Load_Char(face, charCode, FT_LOAD_RENDER) != FT_Err_Ok)
					{
						Console::WriteLine("Failed to load glyph with char code '" + charCode.ToString() +
							"' for font family '" + gcnew String(fontInfo.Family.c_str()) + "', face index " + face->face_index + ".");
					}
					else
					{
						Glyph glyph;

						glyph.Width = face->glyph->bitmap.width;
						glyph.Height = face->glyph->bitmap.rows;
						glyph.BearingX = face->glyph->bitmap_left;
						glyph.BearingY = face->glyph->bitmap_top;
						glyph.Advance = face->glyph->advance.x;
						glyph.CharCode = charCode;
						glyph.ImageData = gcnew array<Byte>(glyph.Width * glyph.Height); // 8 bit (1 byte) per color

						if (glyph.Width > 0 && glyph.Height > 0)
							Marshal::Copy(IntPtr(face->glyph->bitmap.buffer), glyph.ImageData, 0, glyph.ImageData->Length);

						glyphs->Add(glyph);
					}

					charCode = FT_Get_Next_Char(face, charCode, &glyhpIndex);
				}

				fontFace.FaceIndex = face->face_index;
				fontFace.Bold = face->style_flags & FT_STYLE_FLAG_BOLD;
				fontFace.Italic = face->style_flags & FT_STYLE_FLAG_ITALIC;
				fontFace.Glyphs = glyphs->ToArray();

				return fontFace;
			}
		};

		public ref class FreeType
		{
		private:
			FreeTypeInterface* freetype;

		public:
			FreeType()
			{
				freetype = new FreeTypeInterface();
			}

			~FreeType()
			{
				delete freetype;
			}

			Font LoadFont(String^ fontFile, Int32 fontSize)
			{
				return freetype->LoadFont(fontFile, fontSize);
			}

			Font LoadFont(array<Byte>^ data, Int32 fontSize)
			{
				return freetype->LoadFont(data, fontSize);
			}
		};
	}
}
