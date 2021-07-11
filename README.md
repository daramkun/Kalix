## Kalix

`Kalix`는 이미지 일괄 압축 응용프로그램입니다.

### 기능
1. 이미지를 조건부로 PNG, GIF, JPEG, WebP 등으로 압축 지원.
2. 아카이브 파일 내 이미지 압축 지원.
   - 현재는 `ZIP`으로만 지원합니다.
3. 멀티스레드 활용 압축.
   - 현재는 각 스레드 당 파일 할당만 지원합니다.
   - 추후 아카이브 파일 내 파일을 각 스레드에 할당하는 옵션 추가 예정입니다.

### 레퍼런스
- 네이티브 프로세싱 라이브러리 [libdseed](https://github.com/daramkun/libdseed)
- 프로토타입 프로젝트 [Degra](https://github.com/daramkun/Degra)

## Kalix
`Kalix` is Batched Image compression application.

### Features
1. Support to compression `PNG`, `GIF`, `JPEG`, `WebP` with conditions.
2. Support to compression in Archive files.
   - Currently, Output is only support `ZIP`
3. Multi-threaded Compression.
   - Currently only support Thread per Each file.
   - Will support Thread per Archive each file.

### References
- Native Processing Library [libdseed](https://github.com/daramkun/libdseed)
- Prototype Project [Degra](https://github.com/daramkun/Degra)