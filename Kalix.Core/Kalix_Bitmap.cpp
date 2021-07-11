#include "Kalix.h"

dseed::bitmaps::decoder_creator_func g_bitmapDecoders[] = {
	dseed::bitmaps::create_png_bitmap_decoder,
	dseed::bitmaps::create_jpeg_bitmap_decoder,
	dseed::bitmaps::create_gif_bitmap_decoder,
	dseed::bitmaps::create_webp_bitmap_decoder,
	dseed::bitmaps::create_jpeg2000_bitmap_decoder,
	dseed::bitmaps::create_tiff_bitmap_decoder,
	dseed::bitmaps::create_tga_bitmap_decoder,
	dseed::bitmaps::create_dib_bitmap_decoder,
	dseed::bitmaps::create_pkm_bitmap_decoder,
	dseed::bitmaps::create_ktx_bitmap_decoder,
	dseed::bitmaps::create_astc_bitmap_decoder,
	dseed::bitmaps::create_cur_bitmap_decoder,
	dseed::bitmaps::create_ico_bitmap_decoder,
	dseed::bitmaps::create_dds_bitmap_decoder,
	dseed::bitmaps::create_windows_imaging_codec_bitmap_decoder,
};

KALIX_STREAM_OFFSET __conv_origin(dseed::io::seekorigin origin)
{
	switch (origin)
	{
	case dseed::io::seekorigin::begin: return KALIX_STREAM_OFFSET_BEGIN;
	case dseed::io::seekorigin::current: return KALIX_STREAM_OFFSET_CURRENT;
	case dseed::io::seekorigin::end: return KALIX_STREAM_OFFSET_END;
	default: return static_cast<KALIX_STREAM_OFFSET>(-1);
	}
}

class __kalix_stream : public dseed::io::stream
{
public:
	__kalix_stream(KALIX_STREAM* stream)
		: _refCount(1), _stream(*stream)
	{

	}

public:
	static void create_stream(IN KALIX_STREAM* stream, OUT dseed::io::stream** out)
	{
		*out = new __kalix_stream(stream);
	}

public:
	virtual int32_t retain()
	{
		return ++_refCount;
	}
	virtual int32_t release()
	{
		const auto ret = --_refCount;
		if (ret == 0)
			delete this;
		return ret;
	}

public:
	virtual size_t read(void* buffer, size_t length) noexcept
	{
		if (readable())
			return _stream.read(_stream.data, static_cast<BYTE*>(buffer), length);
		return dseed::error_not_impl;
	}
	virtual size_t write(const void* data, size_t length) noexcept
	{
		if (writable())
			return _stream.write(_stream.data, static_cast<const BYTE*>(data), length);
		return dseed::error_not_impl;
	}
	virtual bool seek(dseed::io::seekorigin origin, size_t offset) noexcept
	{
		if (seekable())
			return _stream.seek(_stream.data, __conv_origin(origin), offset);
		return false;
	}
	virtual void flush() noexcept
	{
		if (_stream.flush != nullptr)
			_stream.flush(_stream.data);
	}
	virtual dseed::error_t set_length(size_t length) noexcept { return dseed::error_not_impl; }

public:
	virtual size_t position() noexcept { return _stream.position(_stream.data); }
	virtual size_t length() noexcept { return _stream.length(_stream.data); }

public:
	virtual bool readable() noexcept { return _stream.read != nullptr; }
	virtual bool writable() noexcept { return _stream.write != nullptr; }
	virtual bool seekable() noexcept { return _stream.seek != nullptr; }

private:
	std::atomic<int32_t> _refCount;
	KALIX_STREAM _stream;
};

KALIX_RESULT Kalix_LoadBitmap(IN KALIX_STREAM* inputStream, OUT KALIX_BITMAP* resultBitmap, OUT KALIX_LOAD_FORMAT* loadFormat)
{
	dseed::autoref<dseed::io::stream> in;
	__kalix_stream::create_stream(inputStream, &in);

	for (auto decoder : g_bitmapDecoders)
	{
		auto result = decoder(in, resultBitmap);
		if (dseed::succeeded(result))
		{
			if (decoder == static_cast<dseed::bitmaps::decoder_creator_func>(dseed::bitmaps::create_png_bitmap_decoder))
				*loadFormat = KALIX_LOAD_FORMAT_PUBLIC_NETWORK_GRAPHICS;
			else if (decoder == static_cast<dseed::bitmaps::decoder_creator_func>(dseed::bitmaps::create_gif_bitmap_decoder))
				*loadFormat = KALIX_LOAD_FORMAT_GRAPHICS_INTERCHANGE_FORMAT;
			else if (decoder == static_cast<dseed::bitmaps::decoder_creator_func>(dseed::bitmaps::create_jpeg_bitmap_decoder))
				*loadFormat = KALIX_LOAD_FORMAT_JOINT_PHOTOGRAPHIC_EXPERTS_GROUP;
			else if (decoder == static_cast<dseed::bitmaps::decoder_creator_func>(dseed::bitmaps::create_webp_bitmap_decoder))
				*loadFormat = KALIX_LOAD_FORMAT_WEBP;
			else if (decoder == static_cast<dseed::bitmaps::decoder_creator_func>(dseed::bitmaps::create_windows_imaging_codec_bitmap_decoder))
			{
				dseed::autoref<dseed::bitmaps::bitmap> bitmap;
				if (dseed::succeeded((*resultBitmap)->at(0, &bitmap)))
				{
					dseed::autoref<dseed::attributes> attrs;
					if (dseed::succeeded(bitmap->extra_info(&attrs)))
					{
						dseed::bitmaps::windows_imaging_codec_load_format wicLoadFormat;
						if (dseed::succeeded(attrs->get_int32(dseed::attrkey_wic_load_format, (int32_t*)&wicLoadFormat)))
						{
							switch (wicLoadFormat)
							{
							case dseed::bitmaps::windows_imaging_codec_load_format::png:
								*loadFormat = KALIX_LOAD_FORMAT_PUBLIC_NETWORK_GRAPHICS;
								break;

							case dseed::bitmaps::windows_imaging_codec_load_format::gif:
								*loadFormat = KALIX_LOAD_FORMAT_GRAPHICS_INTERCHANGE_FORMAT;
								break;

							case dseed::bitmaps::windows_imaging_codec_load_format::jpeg:
								*loadFormat = KALIX_LOAD_FORMAT_JOINT_PHOTOGRAPHIC_EXPERTS_GROUP;
								break;

							case dseed::bitmaps::windows_imaging_codec_load_format::webp:
								*loadFormat = KALIX_LOAD_FORMAT_WEBP;
								break;

							default:
								*loadFormat = KALIX_LOAD_FORMAT_UNKNOWN;
								break;
							}
						}
					}
				}
			}
			else
				*loadFormat = KALIX_LOAD_FORMAT_UNKNOWN;

			return KALIX_RESULT_OK;
		}
		in->seek(dseed::io::seekorigin::begin, 0);
	}

	return KALIX_RESULT_CANNOT_LOAD;
}

dseed::error_t create_encoder(dseed::io::stream* stream, KALIX_SAVE_FORMAT saveFormat, IN const KALIX_SAVE_OPTIONS* options, OUT dseed::bitmaps::bitmap_encoder** encoder)
{
	switch (saveFormat)
	{
	case KALIX_SAVE_FORMAT_PUBLIC_NETWORK_GRAPHICS:
	{
		dseed::bitmaps::png_encoder_options option = {};
		return dseed::bitmaps::create_png_bitmap_encoder(stream, &option, encoder);
	}
	case KALIX_SAVE_FORMAT_GRAPHICS_INTERCHANGE_FORMAT:
	{
		return dseed::bitmaps::create_gif_bitmap_encoder(stream, nullptr, encoder);
	}
	case KALIX_SAVE_FORMAT_JOINT_PHOTOGRAPHIC_EXPERTS_GROUP:
	{
		dseed::bitmaps::jpeg_encoder_options option = {};
		option.quality = options->quality;
		return dseed::bitmaps::create_jpeg_bitmap_encoder(stream, &option, encoder);
	}
	case KALIX_SAVE_FORMAT_WEBP:
	{
		dseed::bitmaps::webp_encoder_options option;
		option.quality = options->quality;
		option.lossless = options->use_lossless_compression;
		return dseed::bitmaps::create_webp_bitmap_encoder(stream, &option, encoder);
	}
	default:
		return dseed::error_invalid_args;
	}
}

bool is_support_multiple_plane(KALIX_SAVE_FORMAT saveFormat)
{
	if (saveFormat == KALIX_SAVE_FORMAT_GRAPHICS_INTERCHANGE_FORMAT || saveFormat == KALIX_SAVE_FORMAT_WEBP)
		return true;
	return false;
}

KALIX_RESULT Kalix_SaveBitmap(IN KALIX_STREAM* outputStream, IN KALIX_BITMAP bitmap, IN const KALIX_SAVE_OPTIONS* options, IN KALIX_SAVE_FORMAT saveFormat)
{
	if (saveFormat == KALIX_SAVE_FORMAT_SAME_FORMAT || saveFormat >= KALIX_SAVE_FORMAT_MAX)
		return KALIX_RESULT_CANNOT_SAVE;

	dseed::autoref<dseed::io::stream> out;
	__kalix_stream::create_stream(outputStream, &out);
	
	dseed::autoref<dseed::bitmaps::bitmap_encoder> bitmapEncoder;
	if (dseed::failed(create_encoder(out, saveFormat, options, &bitmapEncoder)))
		return KALIX_RESULT_CANNOT_SAVE;

	for (auto i = 0; is_support_multiple_plane(saveFormat) ? true : i < 1; ++i)
	{
		dseed::autoref<dseed::bitmaps::bitmap> bmp;
		if (dseed::failed(bitmap->at(i, &bmp)))
			break;

		if (dseed::failed(bitmapEncoder->encode_frame(bmp)))
			return KALIX_RESULT_CANNOT_SAVE;
	}

	if (dseed::failed(bitmapEncoder->commit()))
		return KALIX_RESULT_CANNOT_SAVE;

	return KALIX_RESULT_OK;
}

KALIX_RESULT Kalix_FreeBitmap(KALIX_BITMAP resultBitmap)
{
	if (resultBitmap == nullptr)
		return KALIX_RESULT_FAIL;
	resultBitmap->release();
	return KALIX_RESULT_OK;
}