using PropertyTools.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReringProject.Define {

    /// <summary>
    /// 시퀀스의 ID(쓰레드 단위 = 카메라)
    /// </summary>
    public enum ESequence : int {
        
        Corner_Align = 1,
    }

    /// <summary>
    /// 각 시퀀스에 종속되는 action의 ID (쓰레드가 수행할 수 있는 동작 단위)
    /// </summary>
    public enum EAction : int {

        //0319 자재 유무 검사 액션 추가 - 시퀀스 가장 처음에 실행됨
        MaterialCheck = 0,

        Calibration = 1,
        LT_Inspection = 2,
        RT_Inspection = 3,
        LB_Inspection = 4,
        RB_Inspection = 5,

        Unknown = Int32.MaxValue
    }

}
