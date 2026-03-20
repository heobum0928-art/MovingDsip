using System;
using System.Collections.Generic;
using PropertyTools.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReringProject.Define;
using ReringProject.Device;
using OpenCvSharp;
using ReringProject.UI;
using System.Windows;
using ReringProject.Utility;

namespace ReringProject.Sequence
{
    public class CornerAlignInspectionActionContext : ActionContext
    {
        #region fields

        #endregion

        #region propertise

        #endregion

        #region methods

        #endregion

        #region constructors
        public CornerAlignInspectionActionContext(ActionBase source) : base(source)
        {
        }
        #endregion
    }

    public class CornerAlignInspectionParam : CameraSlaveParam
    {
        #region fields
        public readonly int AlgIndex;
        private string _ProccessName;

        private Mat GrayImage = null;


        private System.Windows.Rect _HorzRect;

        private System.Windows.Rect _VertRect;

        #endregion

        #region propertise
        [Category("Common")]
        [DisplayName("Process Name")]
        [ReadOnly(true)]
        public string ProcessName
        {
            get { return _ProccessName; }
            set
            {
                _ProccessName = value;
                RaisePropertyChanged("ProcessName");
            }
        }


        [Category("ROI Setting")]
        [Rectangle, Converter(typeof(UI.RectConverter))]
        public System.Windows.Rect HorzRect
        {
            get
            {
                return _HorzRect;
            }

            set
            {
                _HorzRect = value;
                RaisePropertyChanged("HorzRect");
            }
        }


        [Rectangle, Converter(typeof(UI.RectConverter))]
        public System.Windows.Rect VertRect
        {
            get
            {
                return _VertRect;
            }

            set
            {
                _VertRect = value;
                RaisePropertyChanged("VertRect");
            }
        }


        #endregion

        #region methods

        #endregion

        #region constructors
        public CornerAlignInspectionParam(object owner, int algIndex) : base(owner)
        {
            AlgIndex = algIndex;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override double ConvertPixelToMM(double pixel)
        {
            return base.ConvertPixelToMM(pixel);
        }

        public override bool Load(IniFile loadFile, string groupName)
        {
            return base.Load(loadFile, groupName);
        }

        public override bool Save(IniFile saveFile, string groupName)
        {
            return base.Save(saveFile, groupName);
        }

        public override void PutImage(Mat image)
        {
            if (image == null) return;

            if (GrayImage == null || GrayImage.IsDisposed)
            {
                GrayImage = new Mat(image.Size(), MatType.CV_8UC1);
            }
            else if ((GrayImage.Width != image.Width) || (GrayImage.Height != image.Height))
            {
                GrayImage = new Mat(image.Size(), MatType.CV_8UC1);
            }
            Cv2.CvtColor(image, GrayImage, ColorConversionCodes.BGR2GRAY);

            //// Minho - Mil
            //var result = MilManager.Instance.SetCvScrImage(AlgIndex, GrayImage);
            //if (result != true)
            //{
            //    CustomMessageBox.Show("Fail to PutImage", string.Format("PutImage from Camera : {0}, AlgIndex : {1}", DeviceName, AlgIndex), MessageBoxImage.Error);
            //}
        }

        public override bool CopyTo(ParamBase param)
        {
            if (base.CopyTo(param) == false) return false;

            return true;
        }
        #endregion
    }

    public class CornerAlignInspectionAction : ActionBase
    {
        #region fields
        private readonly int AlgIndex;

        private VirtualCamera _Camera;
        private Mat GrayImage = null;

        private CornerAlignInspectionParam _MyParam;


        public enum EStep
        {
            Grab = 0,

            Processing = 1,

            End = 2,
        }
        #endregion

        #region propertise

        #endregion

        #region methods

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override void FinishAction(EContextResult result)
        {
            base.FinishAction(result);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void OnBegin(SequenceContext prevResult = null)
        {
            base.OnBegin(prevResult);


            
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }

        public override void OnLoad()
        {
            _MyParam.ProcessName = Param.OwnerName;

            //camera property setting
            _Camera = SystemHandler.Handle.Devices[_MyParam.DeviceName];
            if (_Camera != null)
            {
                if (_Camera.Properties == null)
                {
                    CustomMessageBox.Show(_Camera.Name + " Camera Not Open!", "Camera is not open. Please check your connection status.", MessageBoxImage.Error);
                    return;
                }
                if (!_Camera.Properties.ApplyFromParam(_MyParam))
                {
                    CustomMessageBox.Show(_Camera.Name + " Camera Property Set Fail!", "Check camera settings. or camera state.", MessageBoxImage.Error);
                }
                if (!_Camera.SetSoftwareTriggerMode())
                {
                    CustomMessageBox.Show(_Camera.Name + " Camera Software trigger mode Set Fail!", "Check camera settings. or camera state.", MessageBoxImage.Error);
                }
            }
            else
            {
                CustomMessageBox.Show(_MyParam.DeviceName + " Camera Not Open!", "Camera is not open. Please check your connection status.", MessageBoxImage.Error);
                return;
            }

            base.OnLoad();
        }

        public override void OnPaused()
        {
            base.OnPaused();
        }

        public override void OnResume()
        {
            base.OnResume();
        }

        public override void Release()
        {
            base.Release();
        }

        public override ActionContext Run()
        {
            switch ((EStep)Step)
            {
                case EStep.Grab:


                    Step++;
                    break;

                case EStep.Processing:

                    Step++;
                    break;

                case EStep.End:

                    FinishAction(EContextResult.Pass);

                    break;

            }
            return base.Run();
        }

        public override string ToString()
        {
            return base.ToString();
        }
        #endregion

        #region constructors
        public CornerAlignInspectionAction(EAction id, string name, int algIndex) : base(id, name)
        {
            AlgIndex = algIndex;

            Context = new CornerAlignInspectionActionContext(this);

            Param = new CornerAlignInspectionParam(this, algIndex);

            _MyParam = Param as CornerAlignInspectionParam;
        }

        #endregion
    }
}
