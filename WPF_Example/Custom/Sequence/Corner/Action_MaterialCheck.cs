// 0319 자재 유무 검사 액션 - 신규 추가
// 검사 시퀀스 가장 처음에 실행되어 자재가 있는지 없는지 판단함
// 판단 방법: ROI 영역에서 컨투어 검출 후 면적/크기로 자재 유무 판단

using System;
using System.Collections.Generic;
using PropertyTools.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReringProject.Define;
using ReringProject.Device;
using ReringProject.UI;
using ReringProject.Utility;
using ReringProject.Setting;
using OpenCvSharp;
using System.Windows;

namespace ReringProject.Sequence
{
    // 0319 자재 유무 검사 액션 컨텍스트
    public class MaterialCheckActionContext : ActionContext
    {
        #region constructors
        public MaterialCheckActionContext(ActionBase source) : base(source)
        {
        }
        #endregion
    }

    // 0319 자재 유무 검사 파라미터
    public class MaterialCheckParam : CameraSlaveParam
    {
        #region fields
        private string _ProcessName;

        // 0319 컨투어 최소 면적 (픽셀^2, 이 값 이상의 컨투어가 있어야 자재 있음으로 판단)
        private double _MinArea = 5000.0;

        // 0319 자재 예상 너비 (픽셀, 0이면 무시)
        private double _ExpectedWidth = 0.0;

        // 0319 자재 예상 높이 (픽셀, 0이면 무시)
        private double _ExpectedHeight = 0.0;

        // 0319 이진화 최소 임계값 (0~255)
        private int _ThresholdMin = 50;

        // 0319 이진화 최대 임계값 (0~255)
        private int _ThresholdMax = 255;

        // 0319 검사할 ROI 영역 (화면 전체 대비 중앙 영역 기본값)
        private System.Windows.Rect _CheckRect;
        #endregion

        #region propertise

        [Category("Common")]
        [DisplayName("Process Name")]
        [ReadOnly(true)]
        public string ProcessName
        {
            get { return _ProcessName; }
            set
            {
                _ProcessName = value;
                RaisePropertyChanged("ProcessName");
            }
        }

        // 0319 컨투어 최소 면적 프로퍼티
        [Category("Material Check")]
        [DisplayName("Min Area (px^2)")]
        public double MinArea
        {
            get { return _MinArea; }
            set
            {
                _MinArea = value;
                RaisePropertyChanged("MinArea");
            }
        }

        // 0319 예상 너비 프로퍼티 (0이면 크기 검사 미사용)
        [Category("Material Check")]
        [DisplayName("Expected Width (px)")]
        public double ExpectedWidth
        {
            get { return _ExpectedWidth; }
            set
            {
                _ExpectedWidth = value;
                RaisePropertyChanged("ExpectedWidth");
            }
        }

        // 0319 예상 높이 프로퍼티 (0이면 크기 검사 미사용)
        [Category("Material Check")]
        [DisplayName("Expected Height (px)")]
        public double ExpectedHeight
        {
            get { return _ExpectedHeight; }
            set
            {
                _ExpectedHeight = value;
                RaisePropertyChanged("ExpectedHeight");
            }
        }

        // 0319 이진화 최소 임계값 프로퍼티
        [Category("Material Check")]
        [DisplayName("Threshold Min (0~255)")]
        public int ThresholdMin
        {
            get { return _ThresholdMin; }
            set
            {
                _ThresholdMin = Math.Max(0, Math.Min(254, value));
                RaisePropertyChanged("ThresholdMin");
            }
        }

        // 0319 이진화 최대 임계값 프로퍼티
        [Category("Material Check")]
        [DisplayName("Threshold Max (0~255)")]
        public int ThresholdMax
        {
            get { return _ThresholdMax; }
            set
            {
                _ThresholdMax = Math.Max(1, Math.Min(255, value));
                RaisePropertyChanged("ThresholdMax");
            }
        }

        // 0319 검사 ROI 프로퍼티
        [Category("Material Check")]
        [Rectangle, Converter(typeof(UI.RectConverter))]
        public System.Windows.Rect CheckRect
        {
            get { return _CheckRect; }
            set
            {
                _CheckRect = value;
                RaisePropertyChanged("CheckRect");
            }
        }

        #endregion

        #region constructors
        public MaterialCheckParam(object owner) : base(owner)
        {
        }
        #endregion
    }

    // 0319 자재 유무 검사 액션
    public class MaterialCheckAction : ActionBase
    {
        #region fields
        private VirtualCamera _Camera;
        private Mat _GrayImage = null;
        private Mat _BlurImage = null;
        private Mat _BinaryImage = null;
        private MaterialCheckParam _MyParam;

        // 0319 스텝 정의: Grab(촬영) → Processing(컨투어 분석) → End(결과 저장)
        public enum EStep
        {
            Grab = 0,
            Processing = 1,
            End = 2,
        }
        #endregion

        #region methods

        public override void OnLoad()
        {
            _MyParam.ProcessName = Param.OwnerName;

            // 0319 카메라 초기화
            _Camera = SystemHandler.Handle.Devices[_MyParam.DeviceName];
            if (_Camera == null)
            {
                CustomMessageBox.Show(_MyParam.DeviceName + " Camera Not Open!",
                    "Camera is not open. Please check your connection status.",
                    System.Windows.MessageBoxImage.Error);
                return;
            }
            if (_Camera.Properties == null)
            {
                CustomMessageBox.Show(_Camera.Name + " Camera Not Open!",
                    "Camera is not open. Please check your connection status.",
                    System.Windows.MessageBoxImage.Error);
                return;
            }
            if (!_Camera.Properties.ApplyFromParam(_MyParam))
            {
                CustomMessageBox.Show(_Camera.Name + " Camera Property Set Fail!",
                    "Check camera settings. or camera state.",
                    System.Windows.MessageBoxImage.Error);
            }
            if (!_Camera.SetSoftwareTriggerMode())
            {
                CustomMessageBox.Show(_Camera.Name + " Camera Software trigger mode Set Fail!",
                    "Check camera settings. or camera state.",
                    System.Windows.MessageBoxImage.Error);
            }

            base.OnLoad();
        }

        public override ActionContext Run()
        {
            switch ((EStep)Step)
            {
                case EStep.Grab:
                    // 0319 카메라 핸들이 없으면 다시 가져오기
                    if (_Camera == null)
                    {
                        _Camera = SystemHandler.Handle.Devices[_MyParam.DeviceName];
                        if (_Camera == null)
                        {
                            Logging.PrintLog((int)ELogType.Error, "{0} Camera Handle is null!", _MyParam.DeviceName);
                            FinishAction(EContextResult.Error);
                            break;
                        }
                    }

                    // 0319 이미지 촬영
                    Context.ResultImage = _Camera.GrabImage();
                    if (Context.ResultImage == null)
                    {
                        Logging.PrintLog((int)ELogType.Error, "{0} Camera Image Grab Failed!", _MyParam.DeviceName);
                        FinishAction(EContextResult.Error);
                        break;
                    }

                    // 0319 그레이스케일 변환
                    if (_GrayImage == null)
                    {
                        _GrayImage = new Mat(Context.ResultImage.Size(), MatType.CV_8UC1);
                    }
                    else if (_GrayImage.Width != Context.ResultImage.Width || _GrayImage.Height != Context.ResultImage.Height)
                    {
                        _GrayImage.Dispose();
                        _GrayImage = new Mat(Context.ResultImage.Size(), MatType.CV_8UC1);
                    }
                    Cv2.CvtColor(Context.ResultImage, _GrayImage, ColorConversionCodes.BGR2GRAY);

                    Step++;
                    break;

                case EStep.Processing:
                    // 0319 컨투어 기반 자재 유무 판단
                    if (_GrayImage == null || _GrayImage.IsDisposed)
                    {
                        Logging.PrintLog((int)ELogType.Error, "MaterialCheck: GrayImage is null!");
                        FinishAction(EContextResult.Error);
                        break;
                    }

                    // 0319 ROI 설정 - CheckRect가 비어있으면 이미지 전체 사용
                    Mat roiMat;
                    System.Windows.Rect checkRect = _MyParam.CheckRect;
                    bool useRoi = checkRect.Width > 0 && checkRect.Height > 0;
                    if (useRoi)
                    {
                        // 0319 ROI가 이미지 범위를 벗어나지 않도록 클램핑
                        int x      = Math.Max(0, (int)checkRect.X);
                        int y      = Math.Max(0, (int)checkRect.Y);
                        int width  = Math.Min((int)checkRect.Width,  _GrayImage.Width  - x);
                        int height = Math.Min((int)checkRect.Height, _GrayImage.Height - y);
                        roiMat = new Mat(_GrayImage, new OpenCvSharp.Rect(x, y, width, height));
                    }
                    else
                    {
                        // 0319 ROI 미설정 시 전체 이미지 사용
                        roiMat = _GrayImage;
                    }

                    // 0319 GaussianBlur로 노이즈 제거
                    if (_BlurImage == null || _BlurImage.Width != roiMat.Width || _BlurImage.Height != roiMat.Height)
                    {
                        _BlurImage?.Dispose();
                        _BlurImage = new Mat(roiMat.Size(), MatType.CV_8UC1);
                    }
                    Cv2.GaussianBlur(roiMat, _BlurImage, new OpenCvSharp.Size(5, 5), 0);

                    // 0319 이진화 (ThresholdMin ~ ThresholdMax 범위)
                    if (_BinaryImage == null || _BinaryImage.Width != roiMat.Width || _BinaryImage.Height != roiMat.Height)
                    {
                        _BinaryImage?.Dispose();
                        _BinaryImage = new Mat(roiMat.Size(), MatType.CV_8UC1);
                    }
                    Cv2.Threshold(_BlurImage, _BinaryImage, _MyParam.ThresholdMin, _MyParam.ThresholdMax, ThresholdTypes.Binary);

                    // 0319 컨투어 검출
                    OpenCvSharp.Point[][] contours;
                    HierarchyIndex[] hierarchy;
                    Cv2.FindContours(_BinaryImage, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    // 0319 최대 면적 컨투어 찾기
                    double maxArea = 0.0;
                    int maxIdx = -1;
                    for (int i = 0; i < contours.Length; i++)
                    {
                        double area = Cv2.ContourArea(contours[i]);
                        if (area > maxArea)
                        {
                            maxArea = area;
                            maxIdx = i;
                        }
                    }

                    Logging.PrintLog((int)ELogType.Trace,
                        "MaterialCheck: contours={0}, maxArea={1:F1}, minArea={2:F1}",
                        contours.Length, maxArea, _MyParam.MinArea);

                    // 0319 면적 조건 확인
                    if (maxArea < _MyParam.MinArea)
                    {
                        Context.Result = EContextResult.NotExist;
                        Logging.PrintLog((int)ELogType.Trace, "MaterialCheck: 자재 없음 (NotExist) - area too small");
                        Step++;
                        break;
                    }

                    // 0319 크기 조건 확인 (ExpectedWidth/Height > 0 일 때만 검사)
                    if (maxIdx >= 0 && (_MyParam.ExpectedWidth > 0 || _MyParam.ExpectedHeight > 0))
                    {
                        OpenCvSharp.Rect boundRect = Cv2.BoundingRect(contours[maxIdx]);

                        Logging.PrintLog((int)ELogType.Trace,
                            "MaterialCheck: boundRect w={0}, h={1}, expectedW={2:F1}, expectedH={3:F1}",
                            boundRect.Width, boundRect.Height, _MyParam.ExpectedWidth, _MyParam.ExpectedHeight);

                        // 0319 너비 검사 (ExpectedWidth > 0 이면 ±30% 허용)
                        if (_MyParam.ExpectedWidth > 0)
                        {
                            double wRatio = (double)boundRect.Width / _MyParam.ExpectedWidth;
                            if (wRatio < 0.7 || wRatio > 1.3)
                            {
                                Context.Result = EContextResult.NotExist;
                                Logging.PrintLog((int)ELogType.Trace, "MaterialCheck: 자재 없음 (NotExist) - width mismatch ratio={0:F2}", wRatio);
                                Step++;
                                break;
                            }
                        }

                        // 0319 높이 검사 (ExpectedHeight > 0 이면 ±30% 허용)
                        if (_MyParam.ExpectedHeight > 0)
                        {
                            double hRatio = (double)boundRect.Height / _MyParam.ExpectedHeight;
                            if (hRatio < 0.7 || hRatio > 1.3)
                            {
                                Context.Result = EContextResult.NotExist;
                                Logging.PrintLog((int)ELogType.Trace, "MaterialCheck: 자재 없음 (NotExist) - height mismatch ratio={0:F2}", hRatio);
                                Step++;
                                break;
                            }
                        }
                    }

                    // 0319 모든 조건 통과 - 자재 있음
                    Context.Result = EContextResult.Pass;
                    Logging.PrintLog((int)ELogType.Trace, "MaterialCheck: 자재 있음 (Pass) - area={0:F1}", maxArea);

                    Step++;
                    break;

                case EStep.End:
                    // 0319 결과를 컨텍스트에 반영하고 액션 종료
                    FinishAction(Context.Result);
                    break;
            }
            return base.Run();
        }

        public override void Release()
        {
            // 0319 Mat 리소스 해제
            if (_GrayImage != null && !_GrayImage.IsDisposed)
            {
                _GrayImage.Dispose();
                _GrayImage = null;
            }
            if (_BlurImage != null && !_BlurImage.IsDisposed)
            {
                _BlurImage.Dispose();
                _BlurImage = null;
            }
            if (_BinaryImage != null && !_BinaryImage.IsDisposed)
            {
                _BinaryImage.Dispose();
                _BinaryImage = null;
            }
            base.Release();
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }

        public override void OnBegin(SequenceContext prevResult = null)
        {
            base.OnBegin(prevResult);
        }

        public override void OnPaused()
        {
            base.OnPaused();
        }

        public override void OnResume()
        {
            base.OnResume();
        }

        public override void FinishAction(EContextResult result)
        {
            base.FinishAction(result);
        }

        #endregion

        #region constructors
        // 0319 생성자 - MATERIAL_CHECK_CAMERA 를 기본 카메라로 사용
        public MaterialCheckAction(EAction id, string name) : base(id, name)
        {
            Context = new MaterialCheckActionContext(this);
            Param   = new MaterialCheckParam(this);
            _MyParam = Param as MaterialCheckParam;

            // 0319 자재 검사용 카메라 지정
            _MyParam.DeviceName = DeviceHandler.MATERIAL_CHECK_CAMERA;
        }
        #endregion
    }
}
