#include "Kalix.h"

dseed::error_t __image_resize(dseed::bitmaps::bitmap* image, KALIX_RESIZE_FILTER filter, int height, dseed::bitmaps::bitmap** ret)
{
	dseed::bitmaps::resize resize_method;
	switch (filter)
	{
	case KALIX_RESIZE_FILTER_NEAREST: resize_method = dseed::bitmaps::resize::nearest; break;
	case KALIX_RESIZE_FILTER_BILINEAR: resize_method = dseed::bitmaps::resize::bilinear; break;
	case KALIX_RESIZE_FILTER_BICUBIC: resize_method = dseed::bitmaps::resize::bicubic; break;
	case KALIX_RESIZE_FILTER_LANCZOS: resize_method = dseed::bitmaps::resize::lanczos; break;
	default: return dseed::error_invalid_args;
	}

	auto size = image->size();
	auto new_size = dseed::size3i(static_cast<int>(size.width * (height / static_cast<float>(size.height))), height, 1);
	
	return dseed::bitmaps::resize_bitmap(image, resize_method, new_size, ret);
}

dseed::error_t __image_pixel_format_to_palette_8bit(dseed::bitmaps::bitmap* image, dseed::bitmaps::bitmap** ret)
{
	dseed::autoref<dseed::bitmaps::bitmap> bitmap;
	return dseed::bitmaps::reformat_bitmap(image, dseed::color::pixelformat::bgra8_indexed8, ret);
}

dseed::error_t __image_pixel_format_to_grayscale(dseed::bitmaps::bitmap* image, dseed::bitmaps::bitmap** ret)
{
	dseed::autoref<dseed::bitmaps::bitmap> bitmap;
	return dseed::bitmaps::reformat_bitmap(image, dseed::color::pixelformat::r8, ret);
}

KALIX_RESULT Kalix_Process(IN KALIX_BITMAP original, IN const KALIX_PROCESS_OPTIONS* options, KALIX_LOAD_FORMAT loadFormat,
	IN OUT KALIX_SAVE_FORMAT* saveFormat, OUT KALIX_BITMAP* resultBitmap)
{
	std::vector<dseed::autoref<dseed::bitmaps::bitmap>> targets;
	for (size_t i = 0; i < original->size(); ++i)
	{
		dseed::autoref<dseed::bitmaps::bitmap> bitmap;
		if (dseed::failed(original->at(i, &bitmap)))
			continue;

		if (const auto bitmap_size = bitmap->size(); bitmap_size.height > options->maximum_height)
		{
			dseed::autoref<dseed::bitmaps::bitmap> tempBitmap;
			if (dseed::failed(__image_resize(bitmap, options->resize_filter, options->maximum_height, &tempBitmap)))
			{
				targets.push_back(bitmap);
				continue;
			}
			bitmap = tempBitmap;
		}

		dseed::bitmaps::bitmap_properties prop = { };
		if (options->use_indexed_color_only_under_256_colors ||
			options->use_grayscale_only_grayscale ||
			options->no_convert_to_png_if_has_transparent_color)
		{
			const auto result = dseed::bitmaps::determine_bitmap_properties(bitmap, &prop, options->determine_threshold);
			if (result != dseed::error_not_support && dseed::failed(result))
			{
				targets.push_back(bitmap);
				continue;
			}
		}

		if (options->no_convert_to_png_if_has_transparent_color && prop.transparent)
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

		if (*saveFormat == KALIX_SAVE_FORMAT_PUBLIC_NETWORK_GRAPHICS)
		{
			if (options->use_indexed_color ||
				(
					options->use_indexed_color_only_under_256_colors &&
					prop.colours != dseed::bitmaps::colorcount::color_cannot_palettable
				)
			)
			{
				dseed::autoref<dseed::bitmaps::bitmap> tempBitmap;
				if (dseed::failed(__image_pixel_format_to_palette_8bit(bitmap, &tempBitmap)))
				{
					targets.push_back(bitmap);
					continue;
				}
				bitmap = tempBitmap;
			}
		}

		if (*saveFormat == KALIX_SAVE_FORMAT_PUBLIC_NETWORK_GRAPHICS || *saveFormat == KALIX_SAVE_FORMAT_JOINT_PHOTOGRAPHIC_EXPERTS_GROUP)
		{
			if (options->use_grayscale ||
				(
					options->use_grayscale_only_grayscale &&
					prop.grayscale
				)
			)
			{
				dseed::autoref<dseed::bitmaps::bitmap> tempBitmap;
				if (dseed::failed(__image_pixel_format_to_grayscale(bitmap, &tempBitmap)))
				{
					targets.push_back(bitmap);
					continue;
				}
				bitmap = tempBitmap;
			}
		}

		targets.push_back(bitmap);
		bitmap = nullptr;
	}

	if (dseed::failed(dseed::bitmaps::create_bitmap_array(original->type(), targets, resultBitmap)))
		return KALIX_RESULT_CANNOT_PROCESS;

	return KALIX_RESULT_OK;
}