#pragma once

#include <dseed.h>
#include <cstdint>

#define KALIX_EXPORT											__declspec(dllexport)

#ifndef IN
#	define IN
#endif

#ifndef OUT
#	define OUT
#endif

#ifndef INOUT
#	define INOUT
#endif

extern "C"
{
	enum KALIX_STREAM_OFFSET : uint8_t
	{
		KALIX_STREAM_OFFSET_BEGIN,
		KALIX_STREAM_OFFSET_CURRENT,
		KALIX_STREAM_OFFSET_END,
	};

	typedef int64_t(*KALIX_STREAM_READ)(void* data, BYTE* buffer, int64_t length);
	typedef int64_t(*KALIX_STREAM_WRITE)(void* data, const BYTE* buffer, int64_t length);
	typedef void(*KALIX_STREAM_FLUSH)(void* data);
	typedef int64_t(*KALIX_STREAM_POSITION)(void* data);
	typedef int64_t(*KALIX_STREAM_LENGTH)(void* data);
	typedef bool(*KALIX_STREAM_SEEK)(void* data, KALIX_STREAM_OFFSET offset, int64_t pos);

	typedef dseed::bitmaps::bitmap_array* KALIX_BITMAP;

	enum KALIX_RESULT : uint8_t
	{
		KALIX_RESULT_OK,

		KALIX_RESULT_CANNOT_LOAD,
		KALIX_RESULT_CANNOT_SAVE,
		KALIX_RESULT_CANNOT_PROCESS,
		KALIX_RESULT_PROCESS_PASS,
		KALIX_RESULT_FAIL,
	};

	enum KALIX_LOAD_FORMAT : uint8_t
	{
		KALIX_LOAD_FORMAT_UNKNOWN,
		KALIX_LOAD_FORMAT_PUBLIC_NETWORK_GRAPHICS,
		KALIX_LOAD_FORMAT_GRAPHICS_INTERCHANGE_FORMAT,
		KALIX_LOAD_FORMAT_JOINT_PHOTOGRAPHIC_EXPERTS_GROUP,
		KALIX_LOAD_FORMAT_WEBP,
		//KALIX_LOAD_FORMAT_AV1_BASED_IMAGE_FORMAT,
	};

	enum KALIX_SAVE_FORMAT : uint8_t
	{
		KALIX_SAVE_FORMAT_SAME_FORMAT,
		KALIX_SAVE_FORMAT_PUBLIC_NETWORK_GRAPHICS,
		KALIX_SAVE_FORMAT_GRAPHICS_INTERCHANGE_FORMAT,
		KALIX_SAVE_FORMAT_JOINT_PHOTOGRAPHIC_EXPERTS_GROUP,
		KALIX_SAVE_FORMAT_WEBP,
		//KALIX_SAVE_FORMAT_AV1_BASED_IMAGE_FORMAT,

		KALIX_SAVE_FORMAT_MAX = KALIX_SAVE_FORMAT_WEBP,
	};

	enum KALIX_RESIZE_FILTER : int8_t
	{
		KALIX_RESIZE_FILTER_NEAREST,
		KALIX_RESIZE_FILTER_BILINEAR,
		KALIX_RESIZE_FILTER_BICUBIC,
		KALIX_RESIZE_FILTER_LANCZOS,
	};

#pragma pack(push, 1)
	struct KALIX_SAVE_OPTIONS
	{
		int quality;
		bool use_lossless_compression;
	};

	struct KALIX_PROCESS_OPTIONS
	{
		int32_t maximum_height;
		KALIX_RESIZE_FILTER resize_filter;

		bool use_grayscale;
		bool use_grayscale_only_grayscale;

		bool use_indexed_color;
		bool use_indexed_color_only_under_256_colors;

		bool no_convert_to_png_if_has_transparent_color;

		int determine_threshold;
	};

	struct KALIX_EXPORT KALIX_STREAM
	{
		void* data;
		KALIX_STREAM_READ read;
		KALIX_STREAM_WRITE write;
		KALIX_STREAM_FLUSH flush;
		KALIX_STREAM_POSITION position;
		KALIX_STREAM_LENGTH length;
		KALIX_STREAM_SEEK seek;
	};
#pragma pack(pop)

	KALIX_EXPORT bool Kalix_Is_Valid_Options(IN const KALIX_SAVE_OPTIONS* saveOptions, IN const KALIX_PROCESS_OPTIONS* processOptions);

	KALIX_EXPORT KALIX_RESULT Kalix_LoadBitmap(IN KALIX_STREAM* inputStream, OUT KALIX_BITMAP* resultBitmap, OUT KALIX_LOAD_FORMAT* loadFormat);
	KALIX_EXPORT KALIX_RESULT Kalix_SaveBitmap(IN KALIX_STREAM* outputStream, IN KALIX_BITMAP bitmap, IN const KALIX_SAVE_OPTIONS* options, IN KALIX_SAVE_FORMAT saveFormat);
	KALIX_EXPORT KALIX_RESULT Kalix_FreeBitmap(KALIX_BITMAP resultBitmap);

	KALIX_EXPORT KALIX_RESULT Kalix_Process(IN KALIX_BITMAP original, IN const KALIX_PROCESS_OPTIONS* options, KALIX_LOAD_FORMAT loadFormat,
		IN OUT KALIX_SAVE_FORMAT *saveFormat, OUT KALIX_BITMAP* resultBitmap);

	KALIX_EXPORT KALIX_RESULT Kalix_DoConversion(IN KALIX_STREAM* inputStream, IN KALIX_STREAM* outputStream, IN OUT KALIX_SAVE_FORMAT* saveFormat,
		IN const KALIX_SAVE_OPTIONS* saveOptions, IN const KALIX_PROCESS_OPTIONS* processOptions);
}