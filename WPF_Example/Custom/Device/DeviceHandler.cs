using ReringProject.Define;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReringProject.Device {
    /// <summary>
    /// 시스템에서 사용되는 카메라 이름 해상도, 방향 등을 정의합니다.
    /// </summary>
    public sealed partial class DeviceHandler {

        //OCR_READER
        public const string CORNER_ALIGN_CAMERA = "CORNER_ALIGN_CAMERA";

        // OCR_READER
        public const int CORNER_ALIGN_CAMERA_WIDTH = 2058;
        public const int CORNER_ALIGN_CAMERA_HEIGHT = 2456;

        //0319 자재 유무 검사용 카메라 추가
        public const string MATERIAL_CHECK_CAMERA = "MATERIAL_CHECK_CAMERA";
        public const int MATERIAL_CHECK_CAMERA_WIDTH = 2058;
        public const int MATERIAL_CHECK_CAMERA_HEIGHT = 2456;

        // Common
        public const int MAX_WIDTH = CORNER_ALIGN_CAMERA_WIDTH;
        public const int MAX_HEIGHT = CORNER_ALIGN_CAMERA_HEIGHT;

        public const int MIN_ROI_WIDTH = 50;
        public const int MAX_ROI_WIDTH = MAX_WIDTH;

        public const int MIN_ROI_HEIGHT = 50;
        public const int MAX_ROI_HEIGHT = MAX_HEIGHT;

        public const int MIN_CIRCLE_RADIUS = 50;
        public const int MAX_CIRCLE_RADIUS = MAX_WIDTH;

        // OCR_READER Reverse
        public const bool REVERSE_X_DEFAULT = false;
        public const bool REVERSE_Y_DEFAULT = false;


        // OCR_READER Rotate
        public const ERotateAngleType ROTATE_DEFAULT = ERotateAngleType._0;



        // Common
        public const double TICK_EXPOSURE = 0.1;
        public const double MIN_EXPOSURE = 10;
        public const double MAX_EXPOSURE = 100000;

        public const double TICK_GAIN = 0.1;
        public const double MIN_GAIN = 0;
        public const double MAX_GAIN = 95.0;

        public const double TICK_GAMMA = 0.1;
        public const double MIN_GAMMA = 0;
        public const double MAX_GAMMA = 3.99998474121094;

        public const string FILTER_IMAGE = "tiff Files(*.tiff)|*.tiff|bmp Files(*.bmp)|*.bmp|jpg Files(*.jpg)|*.jpg|jpeg Files(*.jpeg)|*.jpeg|png Files(*.png)|*.png|All Files(*.*)|*.*";
        public const string EXTENSION_IMAGE = ".tiff";

        public const string EXTENSION_SAVE_IMAGE = ".jpg";
        public const string FILTER_SAVE_IMAGE = "jpg Files(*.jpg)|*.jpg";

        public const string FILTER_MODEL = "mmf Files(*.mmf)|*.mmf";
        public const string EXTENSION_MODEL = ".mmf";

        public const string EXTENSION_CALIBRATION = ".cal";

        /// <summary>
        /// 이 함수에서 카메라를 정의합니다. 
        /// 함수는 시스템 초기화 시점에 호출됩니다.
        /// </summary>
        private void RegisterRequiredDevices() {

           // Default
           SetRequiredDevice(
                ECameraType.HIK,
                ECaptureImageType.Gray8,
                ETriggerSource.Software,
                CORNER_ALIGN_CAMERA,
                CORNER_ALIGN_CAMERA_WIDTH,
                CORNER_ALIGN_CAMERA_HEIGHT,
                REVERSE_X_DEFAULT,
                REVERSE_Y_DEFAULT,
                ROTATE_DEFAULT);

            //0319 자재 유무 검사용 카메라 등록
            SetRequiredDevice(
                ECameraType.HIK,
                ECaptureImageType.Gray8,
                ETriggerSource.Software,
                MATERIAL_CHECK_CAMERA,
                MATERIAL_CHECK_CAMERA_WIDTH,
                MATERIAL_CHECK_CAMERA_HEIGHT,
                REVERSE_X_DEFAULT,
                REVERSE_Y_DEFAULT,
                ROTATE_DEFAULT);
        }
    }
}
