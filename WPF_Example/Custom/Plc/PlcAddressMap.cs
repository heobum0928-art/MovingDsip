// 260320 hbk - PLC D번지 맵 정의
// 자재 유무 검사 결과 (카메라 5대) MX Component 방식
// [PLC → Vision] 명령 영역 / [Vision → PLC] 상태·결과 영역

namespace ReringProject.Plc
{
    public static class PlcAddressMap
    {
        // ─────────────────────────────────────────
        // [PLC → Vision] 명령 영역
        // ─────────────────────────────────────────
        public const string CMD_COMMAND  = "D0000";  // 명령 코드 (1=SITE_STATUS, 2=TEST)
        public const string CMD_SITE     = "D0001";  // 카메라 번호 (1~5, 0=전체)
        public const string CMD_TYPE     = "D0002";  // 검사 타입 (1=MaterialCheck)
        public const string CMD_TRIGGER  = "D0003";  // 트리거 (0→1 상승엣지 = 실행)

        // ─────────────────────────────────────────
        // [Vision → PLC] 공통 상태 영역
        // ─────────────────────────────────────────
        public const string STS_SITE_STATUS = "D0010";  // 0=READY, 1=BUSY, 2=ERROR
        public const string STS_ACK         = "D0011";  // 트리거 수신 확인

        // ─────────────────────────────────────────
        // [Vision → PLC] 카메라별 결과 영역
        // 카메라 1: D0020~D0024
        // 카메라 2: D0030~D0034
        // 카메라 3: D0040~D0044
        // 카메라 4: D0050~D0054
        // 카메라 5: D0060~D0064
        // 1카메라당 10워드 간격
        // ─────────────────────────────────────────
        public const int  CAM_RESULT_BASE   = 20;    // D0020 시작
        public const int  CAM_RESULT_STRIDE = 10;    // 카메라당 10워드 간격

        // 카메라별 결과 오프셋 (베이스로부터)
        public const int OFFSET_EXIST  = 0;   // 자재 유무 (0=없음, 1=있음)
        public const int OFFSET_WIDTH  = 1;   // Width  (픽셀)
        public const int OFFSET_HEIGHT = 2;   // Height (픽셀)
        public const int OFFSET_AREA   = 3;   // Area   (픽셀² / 10, 정수)
        public const int OFFSET_DONE   = 4;   // 완료 플래그 (1=완료)

        // 카메라 수
        public const int CAMERA_COUNT = 5;

        // ─────────────────────────────────────────
        // 명령 코드 상수
        // ─────────────────────────────────────────
        public const short CMD_CODE_SITE_STATUS = 1;
        public const short CMD_CODE_TEST        = 2;

        // ─────────────────────────────────────────
        // 상태 코드 상수
        // ─────────────────────────────────────────
        public const short STATUS_READY = 0;
        public const short STATUS_BUSY  = 1;
        public const short STATUS_ERROR = 2;

        // ─────────────────────────────────────────
        // 결과 코드 상수
        // ─────────────────────────────────────────
        public const short RESULT_NOT_EXIST = 0;  // 자재 없음
        public const short RESULT_EXIST     = 1;  // 자재 있음

        /// <summary>
        /// 카메라 번호(1~5)로 결과 시작 D번지 문자열 반환
        /// </summary>
        public static string GetCamResultAddress(int camNo)
        {
            int addr = CAM_RESULT_BASE + (camNo - 1) * CAM_RESULT_STRIDE;
            return string.Format("D{0:D4}", addr);
        }
    }
}
