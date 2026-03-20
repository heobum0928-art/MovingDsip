using ReringProject.Device;
using ReringProject.Sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReringProject.Network {
    public enum ESite : int {

        DEFAULT = 1,
    }

    public enum ETestType : int {

        Calibration = 1,

        LT_Inspection = 2,
        RT_Inspection = 3,
        LB_Inspection = 4,
        RB_Inspection = 5,

        Unknown = 999
    }


    /// <summary>
    /// 통신 프로토콜의 zone, site, type 등의 정보를 시스템의 sequence, action, light 이름 등으로 치환하기 위한 map을 구성합니다.
    /// </summary>
    public partial class ResourceMap {
        public void Initialize() {

            // Default Camera
            Add(EResource.Camera, ESite.DEFAULT, DeviceHandler.CORNER_ALIGN_CAMERA);
            // light
            Add(EResource.Light, ESite.DEFAULT, LightHandler.LIGHT_DEFAULT);
            // Sequence
            Add(EResource.Sequence, ESite.DEFAULT, SequenceHandler.SEQ_CORNER_ALIGN);

            // Action Default
            Add(EResource.Action, ESite.DEFAULT, ETestType.Calibration, SequenceHandler.ACT_CALIBRATION);

            Add(EResource.Action, ESite.DEFAULT, ETestType.LT_Inspection, SequenceHandler.ACT_LT_INSPECT);
            Add(EResource.Action, ESite.DEFAULT, ETestType.RT_Inspection, SequenceHandler.ACT_RT_INSPECT);
            Add(EResource.Action, ESite.DEFAULT, ETestType.LB_Inspection, SequenceHandler.ACT_LB_INSPECT);
            Add(EResource.Action, ESite.DEFAULT, ETestType.RB_Inspection, SequenceHandler.ACT_RB_INSPECT);
        }

        public bool SetIdentifier(ref VisionRequestPacket packet) {
            switch (packet.RequestType) {
                case VisionRequestType.Light:
                    LightPacket lightPacket = packet.AsLight();
                    lightPacket.Identifier = Find(EResource.Light, (ESite)lightPacket.Site);
                    //lightPacket.Identifier = Find(EResource.Light, (ESite)lightPacket.Site, (ETestType)lightPacket.TestType);
                    if (lightPacket.On) {
                        lightPacket.Identifier2 = Find(EResource.Action, (ESite)lightPacket.Site, (ETestType)lightPacket.TestType);
                    }
                    // 01.12 else 구문 추가.
                    else
                        lightPacket.Identifier2 = Find(EResource.Action, (ESite)lightPacket.Site, (ETestType)lightPacket.TestType);
                    break;
                case VisionRequestType.RecipeChange:
                case VisionRequestType.RecipeGet:
                    //no identifier
                    break;
                case VisionRequestType.SiteStatus:
                    packet.Identifier = Find(EResource.Sequence, (ESite)packet.Site);
                    break;
                case VisionRequestType.Test:
                    TestPacket testPacket = packet.AsTest();
                    testPacket.Identifier = Find(EResource.Sequence, (ESite)testPacket.Site);
                    testPacket.Identifier2 = Find(EResource.Action, (ESite)testPacket.Site, (ETestType)testPacket.TestType);
                    break;
                case VisionRequestType.Unknown:
                    break;
            }

            return true;
        }
    }

    
}
