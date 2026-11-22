#include <stdio.h>
#include <stdbool.h>
#include <stdint.h>
#include <ctype.h>
#include <string.h>
#include <sys/stat.h>

#ifdef _WIN32
#include <direct.h>
#include <io.h>
#define make_dir(path) _mkdir(path)
#define str_cmp(a, b) (_stricmp((a), (b)) == 0)
#else
#include <glob.h>
#include <strings.h>
#define make_dir(path) mkdir(path, 0755)
#define str_cmp(a, b) (strcasecmp((a), (b)) == 0)
#endif

// JPEG -> BASE
// 
// PNG  -> BASE
//      ├── CRC32 -> BASE
//      └── ZLIB  -> BASE
//                ├── ADLER32 -> BASE
//                └── DEFLATE -> BASE
// 
// WEBP -> BASE
//      └── VP8 -> BASE
#define WUFFS_IMPLEMENTATION
#define WUFFS_CONFIG__ENABLE_DROP_IN_REPLACEMENT__STB
#define WUFFS_CONFIG__MODULES
#define WUFFS_CONFIG__MODULE__BASE
#define WUFFS_CONFIG__MODULE__JPEG
#define WUFFS_CONFIG__MODULE__CRC32
#define WUFFS_CONFIG__MODULE__ADLER32
#define WUFFS_CONFIG__MODULE__DEFLATE
#define WUFFS_CONFIG__MODULE__ZLIB
#define WUFFS_CONFIG__MODULE__PNG
#define WUFFS_CONFIG__MODULE__VP8
#define WUFFS_CONFIG__MODULE__WEBP
#include "wuffs-v0.4.c"

#include "webp/encode.h"

// SETTING_QUALITY: 0 - 100, depends on the use case (lower = higher compression).
// SETTING_METHOD: 0 - 6, 6 is maximum compression, but takes more time (~100 ms).
// MAX_IMAGE_PIXELS: images larger than this are rejected before decoding.
#define SETTING_QUALITY 10.0f
#define SETTING_METHOD 6
#define MAX_IMAGE_PIXELS 25000000LL
#define OUTPUT_FOLDER "output"
#define PATH_BUFFER_SIZE (1 << 10)

static bool is_force_convert = 0;

typedef struct{
    const char *name;
    const char *ext;
} PathInfo;

static bool ask_force_convert(void){
    char answer[3];
    printf("If source image size is better than after convert, do you still want to convert? (Y/N): ");
    fflush(stdout);
    
    if (fgets(answer, sizeof(answer), stdin) == NULL){
        return 0;
    }
    
    return tolower((unsigned char) answer[0]) == 'y' && (answer[1] == '\n' || answer[1] == '\0');
}

static PathInfo parse_path(const char *path){
    PathInfo info = {
        path,
        NULL
    };
    // O(2N) => O(N)
    for (const char *i = path; *i; i++){
        if(*i == '/' || *i == '\\'){
            info.name = i + 1;
            info.ext = NULL;
        }
        else if (*i == '.'){
            info.ext = i;
        }
    }

    return info;
}

static int encode_webp(const uint8_t *rgba, int width, int height, WebPMemoryWriter *writer){
    WebPConfig config;
    WebPPicture picture;
    if (!WebPConfigInit(&config) || !WebPPictureInit(&picture)) return 0;

    config.quality = SETTING_QUALITY;
    config.method = SETTING_METHOD;
    config.thread_level = 1;

    picture.use_argb = 1;
    picture.width = width;
    picture.height = height;
    picture.writer = WebPMemoryWrite;
    picture.custom_ptr = writer;

    int result = WebPPictureImportRGBA(&picture, rgba, width * 4) && WebPEncode(&config, &picture);
    WebPPictureFree(&picture);

    return result;
}

static bool ext_verify(const char *ext){
    if (ext == NULL) return 0;
    return str_cmp(ext, ".jpg") || str_cmp(ext, ".jpeg") || str_cmp(ext, ".webp");
}

static void convert_webp(const char *path){
    int width, height, channels;

    if (!stbi_info(path, &width, &height, &channels)){
        printf("Failed: %s (%s)\n", path, stbi_failure_reason());
        return;
    }

    long long pixels = (long long) width * height;
    if (pixels > MAX_IMAGE_PIXELS){
        printf("Failed: %s (Image size (%lld pixels) exceeds limit of %lld pixels)\n",
            path, pixels, MAX_IMAGE_PIXELS);
        return;
    }

    stbi_uc *rgba = stbi_load(path, &width, &height, &channels, STBI_rgb_alpha);
    if (!rgba){
        printf("Failed: %s (%s)\n", path, stbi_failure_reason());
        return;
    }

    WebPMemoryWriter writer;
    WebPMemoryWriterInit(&writer);
    int is_pass = encode_webp(rgba, width, height, &writer);
    stbi_image_free(rgba);

    if (!is_pass){
        printf("Failed: %s (WebP encoding failed)\n", path);
        WebPMemoryWriterClear(&writer);
        return;
    }

    PathInfo info = parse_path(path);
    struct stat source;
    if (ext_verify(info.ext) && !is_force_convert && stat(path, &source) == 0
        && (long long) writer.size >= (long long) source.st_size)
        {
        printf("Skip: %s (webp) is not smaller.\n", path);
        WebPMemoryWriterClear(&writer);
        return;
    }

    char webp_path[PATH_BUFFER_SIZE];
    int  name_length = info.ext? (int) (info.ext - info.name): (int) (strlen(info.name));
    snprintf(webp_path, sizeof(webp_path), "%s/%.*s.webp", OUTPUT_FOLDER, name_length, info.name);

    FILE *f = fopen(webp_path, "wb");
    if (!f || fwrite(writer.mem, 1, writer.size, f) != writer.size){
        printf("Failed: %s (cannot write %s)\n", path, webp_path);
    }

    if (f){
        fclose(f);
    }
    WebPMemoryWriterClear(&writer);
}

static void process_pattern(const char *pattern){
    bool is_found = 0;

    #ifdef _WIN32
        PathInfo info = parse_path(pattern);
        int dir_length = (int) (info.name - pattern);

        struct _finddata_t data;
        intptr_t result = _findfirst(pattern, &data);

        if (result != -1){
            do{
                if (data.attrib & _A_SUBDIR) continue;

                char path[PATH_BUFFER_SIZE];
                snprintf(path, sizeof(path), "%.*s%s", dir_length, pattern, data.name);
                convert_webp(path);

                is_found = 1;
            }
            while (_findnext(result, &data) == 0);
            _findclose(result);
        }
    #else
        glob_t result;
        struct stat info;

        if (glob(pattern, 0, NULL, &result) == 0){
            for (size_t i = 0; i < result.gl_pathc; i++){
                if (stat(result.gl_pathv[i], &info) != 0 || !S_ISREG(info.st_mode)) continue;

                convert_webp(result.gl_pathv[i]);
                is_found = 1;
            }
            globfree(&result);
        }
    #endif

    if (!is_found){
        printf("%s is not found.\n", pattern);
    }
}

int main(int argc, char *argv[]){
    is_force_convert = ask_force_convert();
    make_dir(OUTPUT_FOLDER);

    for (int i = 1; i < argc; i++){
        process_pattern(argv[i]);
    }

    return 0;
}