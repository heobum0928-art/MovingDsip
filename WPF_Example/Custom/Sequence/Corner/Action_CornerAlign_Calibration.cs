using System;
using System.Collections.Generic;
using PropertyTools.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReringProject.Define;
using ReringProject.Utility;
using ReringProject.Device;
using ReringProject.UI;
using OpenCvSharp;
using System.Windows;
using ReringProject.Setting;

namespace ReringProject.Sequence
{
    public class CornerAlignCalibrationContext : ActionContext
    {
        #region fields

        #endregion

        #region propertise

        #endregion

        #region methods
        public override void Clear()
        {
            base.Clear();
        }
        #endregion

        #region constructors
        public CornerAlignCalibrationContext(ActionBase source) : base(source)
        {
        }
        #endregion
    }


    public class CornerAlignCalibrationParam : CameraSlaveParam
    {
        #region fields
        public readonly int AlgIndex;

        private string _ProccessName;
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

        #endregion

        #region methods
        public override bool Load(IniFile loadFile, string groupName)
        {
            bool result = base.Load(loadFile, groupName);

            string recipeName = SystemHandler.Handle.Setting.CurrentRecipeName;
            string seqName = SystemHandler.Handle.Sequences[AlgIndex].Name;
            //string strCalibrationFile = RecipeFiles.Handle.GetCalibrationFilePath(recipeName, seqName, "Calibration", "WaferScan" + (CalibrationIndex + 1).ToString());


            return result;
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

        public override bool Save(IniFile saveFile, string groupName)
        {
            return base.Save(saveFile, groupName);
        }

        public override void PutImage(Mat image)
        {
            base.PutImage(image);
        }

        public override bool CopyTo(ParamBase param)
        {
            return base.CopyTo(param);
        }
        #endregion

        #region constructors
        public CornerAlignCalibrationParam(object owner, int algIndex) : base(owner)
        {
            AlgIndex = algIndex;
        }
        #endregion
    }

    public class CornerAlignCalibrationAction : ActionBase
    {
        #region fields
        private readonly int AlgIndex;

        private CornerAlignCalibrationParam _MyParam;

        private VirtualCamera _Camera;
        private Mat GrayImage = null;


        public enum EStep
        {
            Grab = 0,

            End = 1,
        }


        #endregion

        #region propertise

        #endregion

        #region methods
        public override void OnBegin(SequenceContext prevResult = null)
        {

            string ActName = Param.OwnerName;
            //if (ActName == "Inspect_Left")
            //    pMyContext.ProcessName = pMyParam.ProcessName = "Left WAFER";
            //else if (ActName == "Inspect_Right")
            //    pMyContext.ProcessName = pMyParam.ProcessName = "Right WAFER";
            //else
            //    pMyContext.ProcessName = pMyParam.ProcessName = "None";





            base.OnBegin(prevResult);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override void OnLoad()
        {

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

        public override void OnEnd()
        {
            base.OnEnd();
        }

        public override void OnPaused()
        {
            base.OnPaused();
        }

        public override void OnResume()
        {
            base.OnResume();
        }

        public override ActionContext Run()
        {
            switch((EStep)Step)
            {
                case EStep.Grab:

                    if (_Camera == null)
                    {
                        _Camera = SystemHandler.Handle.Devices[_MyParam.DeviceName];
                        if (_Camera != null)
                        {
                            if (!_Camera.Properties.ApplyFromParam(_MyParam))
                            {
                                Logging.PrintLog((int)ELogType.Error, "{0} Camera Set Property Fail!", _Camera.Name);
                            }
                            if (!_Camera.SetSoftwareTriggerMode())
                            {
                                Logging.PrintLog((int)ELogType.Error, "{0} Camera Set Trigger mode Fail!", _Camera.Name);
                            }
                        }
                        else
                        {
                            Logging.PrintLog((int)ELogType.Error, "{0} Camera Handle is null!", _MyParam.DeviceName);
                            FinishAction(EContextResult.Error);
                            break;
                        }
                    }
                    Context.ResultImage = _Camera.GrabImage();
                    if (Context.ResultImage == null)
                    {
                        Logging.PrintLog((int)ELogType.Error, "{0} Camera Image Grab Failed!", _MyParam.DeviceName);

                        FinishAction(EContextResult.Error);
                        break;
                    }
                    if (GrayImage == null)
                    {
                        GrayImage = new Mat(Context.ResultImage.Size(), MatType.CV_8UC1);
                    }
                    else if ((GrayImage.Width != Context.ResultImage.Width) || (GrayImage.Height != Context.ResultImage.Height))
                    {
                        GrayImage.Dispose();
                        GrayImage = new Mat(Context.ResultImage.Size(), MatType.CV_8UC1);
                    }
                    Cv2.CvtColor(Context.ResultImage, GrayImage, ColorConversionCodes.BGR2GRAY);

                    //// Minho - Mil
                    //var result = MilManager.Instance.SetCvScrImage(AlgIndex, GrayImage);
                    //if (result != true)
                    //{
                    //    Logging.PrintLog((int)ELogType.Error, "Failed {0} in {1} ReturnCode:{2}", "agtAM_PutImage", ID.ToString(), result);

                    //    FinishAction(EContextResult.Error);
                    //    break;
                    //}
                    Step++;
                    break;

                case EStep.End:

                    break;
            }
            return base.Run();
        }

        public override void Release()
        {
            base.Release();
        }

        public override void FinishAction(EContextResult result)
        {
            base.FinishAction(result);
        }
        #endregion

        #region constructors
        public CornerAlignCalibrationAction(EAction id, string name, int algIndex) : base(id, name)
        {
            AlgIndex = algIndex;

            Context = new CornerAlignCalibrationContext(this);

            Param = new CornerAlignCalibrationParam(this, algIndex);

            _MyParam = Param as CornerAlignCalibrationParam;

        }
        #endregion
    }
}
