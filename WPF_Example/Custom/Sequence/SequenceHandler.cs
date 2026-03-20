using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReringProject.Define;
using ReringProject.Device;

namespace ReringProject.Sequence {
    public sealed partial class SequenceHandler {

        public const string SEQ_CORNER_ALIGN = "SEQ_CORNER_ALIGN";
        public const int DEFAULT_Alg_index = 0;

        //0319 자재 유무 검사 액션 이름 추가
        public const string ACT_MATERIAL_CHECK = "MaterialCheck";

        public const string ACT_CALIBRATION = "Calibration";

        public const string ACT_LT_INSPECT = "LT_Inspect";
        public const string ACT_RT_INSPECT = "RT_Inspect";
        public const string ACT_LB_INSPECT = "LB_Inspect";
        public const string ACT_RB_INSPECT = "RB_Inspect";

        public const int Calibration_Alg_Index = 0;

        public const int LT_Inspection_Alg_Index = 1;
        public const int RT_Inspection_Alg_Index = 2;
        public const int LB_Inspection_Alg_Index = 3;
        public const int RB_Inspection_Alg_Index = 4;

        //Model & Pattern Index
        //pattern
        //public const int Inspection_Model_Index = 0;
        //public const int Teaching_Model_Index = 1;


        /// <summary>
        /// Sequence를 정의합니다.
        /// </summary>
        private void RegisterSequences() {

            SequenceBuilder.RegisterSequence(
                new CornerAlignSequence(ESequence.Corner_Align, SEQ_CORNER_ALIGN, DeviceHandler.CORNER_ALIGN_CAMERA, LightHandler.LIGHT_DEFAULT)
                );
        }

        private void RegisterActions() {

            SequenceBuilder.RegisterAction(

                //0319 자재 유무 검사 액션 등록 - 가장 먼저 실행됨
                new MaterialCheckAction(EAction.MaterialCheck, ACT_MATERIAL_CHECK),

                new CornerAlignCalibrationAction(EAction.Calibration, ACT_CALIBRATION, Calibration_Alg_Index),

                new CornerAlignInspectionAction(EAction.LT_Inspection, ACT_LT_INSPECT, LT_Inspection_Alg_Index),
                new CornerAlignInspectionAction(EAction.RT_Inspection, ACT_RT_INSPECT, RT_Inspection_Alg_Index),

                new CornerAlignInspectionAction(EAction.LB_Inspection, ACT_LB_INSPECT, LB_Inspection_Alg_Index),
                new CornerAlignInspectionAction(EAction.RB_Inspection, ACT_RB_INSPECT, RB_Inspection_Alg_Index)

                );
        }
        
        /// <summary>
        /// Sequence와 Action의 관계를 정의합니다. (Sequence -> Action 관계입니다.)
        /// </summary>
        private void InitializeSequences() {
            SequenceBuilder seq;

            seq = SequenceBuilder.CreateSequence(ESequence.Corner_Align);
            //0319 MaterialCheck를 맨 앞에 추가 - 자재 없으면 이후 검사 진행 안 함
            seq.AddAction(EAction.MaterialCheck, EAction.Calibration, EAction.LT_Inspection, EAction.RT_Inspection, EAction.LB_Inspection, EAction.RB_Inspection);
            RegisterSequence(seq);
        }
    }
}
