#include "Kalix.h"

bool Kalix_Is_Valid_Options(IN const KALIX_SAVE_OPTIONS* saveOptions, IN const KALIX_PROCESS_OPTIONS* processOptions)
{
	bool ret = true;
	
	if (saveOptions)
	{
		ret = ret && (saveOptions->quality >= 0 && saveOptions->quality <= 100);
	}

	if (processOptions)
	{
		ret = ret && ((processOptions->maximum_height >= 1 && processOptions->maximum_height <= 65535) || processOptions->maximum_height == -1);
		if (processOptions->use_grayscale || processOptions->use_grayscale_only_grayscale)
			ret = ret && (processOptions->use_grayscale != processOptions->use_grayscale_only_grayscale);
		if (processOptions->use_indexed_color || processOptions->use_indexed_color_only_under_256_colors)
			ret = ret && (processOptions->use_indexed_color != processOptions->use_indexed_color_only_under_256_colors);
	}

	return ret;
}

KALIX_RESULT Kalix_DoConversion(IN KALIX_STREAM* inputStream, IN KALIX_STREAM* outputStream, IN OUT KALIX_SAVE_FORMAT* saveFormat,
	IN const KALIX_SAVE_OPTIONS* saveOptions, IN const KALIX_PROCESS_OPTIONS* processOptions)
{
	dseed::autoref<dseed::bitmaps::bitmap_array> bitmap;
	KALIX_LOAD_FORMAT loadFormat;
	if (Kalix_LoadBitmap(inputStream, &bitmap, &loadFormat) != KALIX_RESULT_OK)
		return KALIX_RESULT_CANNOT_LOAD;

	if (*saveFormat == KALIX_SAVE_FORMAT_SAME_FORMAT)
	{
		switch (loadFormat)
		{
		case KALIX_LOAD_FORMAT_PUBLIC_NETWORK_GRAPHICS:
			*saveFormat = KALIX_SAVE_FORMAT_PUBLIC_NETWORK_GRAPHICS;
			break;

		case KALIX_LOAD_FORMAT_GRAPHICS_INTERCHANGE_FORMAT:
			*saveFormat = KALIX_SAVE_FORMAT_GRAPHICS_INTERCHANGE_FORMAT;
			break;

		case KALIX_LOAD_FORMAT_JOINT_PHOTOGRAPHIC_EXPERTS_GROUP:
			*saveFormat = KALIX_SAVE_FORMAT_JOINT_PHOTOGRAPHIC_EXPERTS_GROUP;
			break;

		case KALIX_LOAD_FORMAT_WEBP:
			*saveFormat = KALIX_SAVE_FORMAT_WEBP;
			break;

		default:
			return KALIX_RESULT_PROCESS_PASS;
		}
	}

	dseed::autoref<dseed::bitmaps::bitmap_array> proceedBitmap;
	if (const auto processResult = Kalix_Process(bitmap, processOptions, loadFormat, saveFormat, &proceedBitmap); processResult != KALIX_RESULT_OK)
		return processResult;

	return Kalix_SaveBitmap(outputStream, proceedBitmap, saveOptions, *saveFormat);
}