using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ReringProject.Define;
using ReringProject.Device;
using ReringProject.UI;

namespace ReringProject.Sequence
{
    public class CornerAlignSequenceContext : SequenceContext
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

        public override void CopyFrom(ActionContext actionContext)
        {
            base.CopyFrom(actionContext);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void RenderResult(DrawingContext dc)
        {
            base.RenderResult(dc);
        }

        public override string ToString()
        {
            return base.ToString();
        }
        #endregion

        #region constructors
        public CornerAlignSequenceContext(SequenceBase source) : base(source)
        {

        }
        #endregion
    }

    public class CornerAlignSequence : SequenceBase
    {
        #region fields
        private DeviceHandler pDevs;
        private VirtualCamera pCam;

        private SequenceContext _MyContext;
        private CameraMasterParam _MyParam;

        private readonly string DefaultCamera;
        private readonly string DefaultLight;
        #endregion

        #region propertise

        #endregion

        #region methods
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
            _MyParam.LightGroupName = DefaultLight;
            _MyParam.DeviceName = DefaultCamera;

            pCam = pDevs[_MyParam.DeviceName];

            if (pCam == null)
            {
                // occurs error
                CustomMessageBox.Show("Error", string.Format("Camera {0} - Initialize Fail", _MyParam.DeviceName), System.Windows.MessageBoxImage.Error);
                IsInitialized = false;
                Context.State = EContextState.Error;
                return;
            }
            if (pCam.Properties == null)
            {
                //occurs error
                CustomMessageBox.Show("Error", string.Format("Camera Property {0} - Initialize Fail", _MyParam.DeviceName), System.Windows.MessageBoxImage.Error);
                IsInitialized = false;
                Context.State = EContextState.Error;
                return;
            }

            IsInitialized = true;

            base.OnCreate();
        }

        public override void OnRelease()
        {
            base.OnRelease();
        }

        public override void OnLoad()
        {
            if (!SystemHandler.Handle.Lights.ApplyLight(_MyParam))
            {
            }
            base.OnLoad();
        }

        protected override void AddResponse()
        {
            base.AddResponse();
        }

        public override string ToString()
        {
            return base.ToString();
        }
        #endregion

        #region constructors
        public CornerAlignSequence(ESequence id, string name, string defaultCamera, string defaultLight) : base(id, name)
        {
            pDevs = SystemHandler.Handle.Devices;

            Context = new CornerAlignSequenceContext(this);
            _MyContext = Context as CornerAlignSequenceContext;

            Param = new CameraMasterParam(this);
            _MyParam = Param as CameraMasterParam;


            DefaultLight = defaultLight;
            DefaultCamera = defaultCamera;
        }


        #endregion
    }
}
