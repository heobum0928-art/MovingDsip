// 260320 hbk - PLC MX Component 연동 핸들러
// VisionProtocol_Auto_PRDT_V1.0 (TCP 방식) → MX Component D번지 방식으로 변환
// PLC D번지를 주기적으로 폴링하여 트리거 감지 → 시퀀스 실행 → 카메라별 자재검사 결과 기록
//
// [결과 D번지 구조] 카메라 1~5
//   D0020 + (camNo-1)*10 + 0 : 자재유무 (0=없음, 1=있음)
//   D0020 + (camNo-1)*10 + 1 : Width  (픽셀)
//   D0020 + (camNo-1)*10 + 2 : Height (픽셀)
//   D0020 + (camNo-1)*10 + 3 : Area   (픽셀² / 10)
//   D0020 + (camNo-1)*10 + 4 : Done   (1=완료)

using System;
using System.Threading;
using Project.HWC;
using Project.BaseLib.Enums;
using ReringProject.Define;
using ReringProject.Sequence;
using ReringProject.Setting;
using ReringProject.Utility;

namespace ReringProject.Plc
{
    /// <summary>
    /// 카메라 1대의 자재 검사 결과
    /// </summary>
    public class MaterialCheckResult
    {
        public int  CamNo   { get; set; }
        public bool Exist   { get; set; }   // 자재 있음 여부
        public int  Width   { get; set; }   // 검출된 컨투어 너비 (픽셀)
        public int  Height  { get; set; }   // 검출된 컨투어 높이 (픽셀)
        public double Area  { get; set; }   // 검출된 컨투어 면적 (픽셀²)
    }

    public class PlcHandler : IDisposable
    {
        #region fields
        // 260320 hbk - SIMUL_MODE: DeviceMXComponentXSim, 실 장비: DeviceMXComponentX64
        private IDeviceMxComponentX64 _device;

        private Thread _pollingThread;
        private bool _isTerminated = false;

        // 트리거 상승엣지 감지용 이전 값
        private short _prevTrigger = 0;

        // 현재 요청 중인 카메라 번호 (0=전체)
        private short _currentSite = 0;

        // 폴링 주기 (ms)
        private const int POLLING_INTERVAL_MS = 50;

        // LogicalStationNumber (MX Component 설정값)
        public int LogicalStationNumber { get; set; } = 3;
        #endregion

        #region initialize / dispose
        public bool Initialize()
        {
#if SIMUL_MODE
            //260320 hbk - SIMUL_MODE: 시뮬레이터 디바이스 사용 (PLC 없이 테스트)
            _device = new DeviceMXComponentXSim();
#else
            //260320 hbk - 실 장비: MX Component X64 사용
            var dev = new DeviceMXComponentX64();
            dev.LogicalStationNumber = LogicalStationNumber;
            _device = dev;
#endif
            if (!_device.Connect())
            {
                Logging.PrintLog((int)ELogType.Error, "[PlcHandler] Device Connect Failed.");
                return false;
            }

            // 초기 상태 READY, 전체 결과 초기화
            WriteStatus(PlcAddressMap.STATUS_READY);
            ClearAllResults();

            // 폴링 스레드 시작
            _pollingThread = new Thread(PollExecute);
            _pollingThread.IsBackground = true;
            _pollingThread.Name = "PlcHandler_Poll";
            _pollingThread.Start();

            Logging.PrintLog((int)ELogType.Trace, "[PlcHandler] Initialized. Mode={0}",
#if SIMUL_MODE
                "SIMUL"
#else
                "REAL (LSN=" + LogicalStationNumber + ")"
#endif
            );

            // 시퀀스 완료/에러 이벤트 구독
            for (int i = 0; i < SystemHandler.Handle.Sequences.Count; i++)
            {
                SystemHandler.Handle.Sequences[i].OnStop  += OnSequenceStop;
                SystemHandler.Handle.Sequences[i].OnError += OnSequenceError;
            }

            return true;
        }

        public void Dispose()
        {
            _isTerminated = true;
            _pollingThread?.Join(1000);
            _device?.Disconnect();
            Logging.PrintLog((int)ELogType.Trace, "[PlcHandler] Disposed.");
        }
        #endregion

        #region polling
        private void PollExecute()
        {
            while (!_isTerminated)
            {
                try { Poll(); }
                catch (Exception ex)
                {
                    Logging.PrintLog((int)ELogType.Error, "[PlcHandler] Poll Exception: {0}", ex.Message);
                }
                Thread.Sleep(POLLING_INTERVAL_MS);
            }
        }

        private void Poll()
        {
            // D0000~D0003 (Command, Site, Type, Trigger) 4워드 읽기
            short[] cmdData = new short[4];
            if (!_device.BReceive(PlcAddressMap.CMD_COMMAND, 4, ref cmdData)) return;

            short command = cmdData[0];
            short site    = cmdData[1];
            short type    = cmdData[2];
            short trigger = cmdData[3];

            // 260320 hbk - 상승엣지 감지 (0→1)
            if (_prevTrigger == 0 && trigger == 1)
            {
                Logging.PrintLog((int)ELogType.Trace,
                    "[PlcHandler] Trigger Rising. CMD={0}, Site={1}, Type={2}", command, site, type);
                OnTriggerRising(command, site, type);
            }
            _prevTrigger = trigger;
        }
        #endregion

        #region command processing
        private void OnTriggerRising(short command, short site, short type)
        {
            WriteAck(1);  // 트리거 수신 ACK

            switch (command)
            {
                case PlcAddressMap.CMD_CODE_SITE_STATUS:
                    ProcessSiteStatus(site);
                    break;

                case PlcAddressMap.CMD_CODE_TEST:
                    // 260320 hbk - 자재 유무 검사 실행
                    ProcessTest(site, type);
                    break;

                default:
                    Logging.PrintLog((int)ELogType.Error, "[PlcHandler] Unknown CMD={0}", command);
                    break;
            }
        }

        private void ProcessSiteStatus(short site)
        {
            EContextState state = SystemHandler.Handle.Sequences.GetSequenceState(ESequence.Corner_Align);
            short status;
            switch (state)
            {
                case EContextState.Running:
                case EContextState.Paused:
                    status = PlcAddressMap.STATUS_BUSY;  break;
                case EContextState.Error:
                    status = PlcAddressMap.STATUS_ERROR; break;
                default:
                    status = PlcAddressMap.STATUS_READY; break;
            }
            WriteStatus(status);
            Logging.PrintLog((int)ELogType.Trace, "[PlcHandler] SITE_STATUS Site={0} → {1}", site, status);
        }

        private void ProcessTest(short site, short type)
        {
            // 이미 실행 중이면 무시
            EContextState state = SystemHandler.Handle.Sequences.GetSequenceState(ESequence.Corner_Align);
            if (state == EContextState.Running)
            {
                Logging.PrintLog((int)ELogType.Error,
                    "[PlcHandler] TEST ignored - already running. Site={0}", site);
                return;
            }

            _currentSite = site;

            WriteStatus(PlcAddressMap.STATUS_BUSY);

            // 260320 hbk - 해당 카메라 결과 초기화 후 검사 시작
            if (site == 0)
                ClearAllResults();
            else
                ClearCamResult(site);

            // MaterialCheck 액션부터 시작
            bool started = SystemHandler.Handle.Sequences.Start(ESequence.Corner_Align, EAction.MaterialCheck);
            if (!started)
            {
                Logging.PrintLog((int)ELogType.Error, "[PlcHandler] Sequence Start Failed. Site={0}", site);
                WriteStatus(PlcAddressMap.STATUS_ERROR);
            }
            else
            {
                Logging.PrintLog((int)ELogType.Trace, "[PlcHandler] Sequence Started. Site={0}", site);
            }
        }
        #endregion

        #region sequence event handlers
        private void OnSequenceStop(SequenceContext context)
        {
            // 260320 hbk - 시퀀스 완료 → MaterialCheckAction 결과를 D번지에 기록
            SequenceBase seq = context?.Source as SequenceBase;
            if (seq == null) return;

            EContextResult result = context?.Result ?? EContextResult.None;

            // 260320 hbk - CornerAlignSequenceContext에서 Width/Height/Area 읽기
            //              CopyFrom()을 통해 MaterialCheckActionContext 값이 복사되어 있음
            int    width  = 0;
            int    height = 0;
            double area   = 0.0;

            if (context is CornerAlignSequenceContext cornerCtx)
            {
                width  = cornerCtx.MatWidth;
                height = cornerCtx.MatHeight;
                area   = cornerCtx.MatArea;
            }

            var matResult = new MaterialCheckResult
            {
                CamNo  = _currentSite,
                Exist  = (result == EContextResult.Pass),
                Width  = width,
                Height = height,
                Area   = area
            };

            WriteCamResult(matResult);
            WriteStatus(PlcAddressMap.STATUS_READY);
            WriteAck(0);

            Logging.PrintLog((int)ELogType.Trace,
                "[PlcHandler] Done. CamNo={0}, Exist={1}, W={2}, H={3}, Area={4:F1}",
                matResult.CamNo, matResult.Exist, matResult.Width, matResult.Height, matResult.Area);
        }

        private void OnSequenceError(SequenceContext context)
        {
            WriteStatus(PlcAddressMap.STATUS_ERROR);
            WriteAck(0);
            Logging.PrintLog((int)ELogType.Error, "[PlcHandler] Sequence Error.");
        }
        #endregion

        #region plc write helpers
        private void WriteStatus(short status)
        {
            short[] data = { status };
            _device.BSend(PlcAddressMap.STS_SITE_STATUS, 1, ref data);
        }

        private void WriteAck(short ack)
        {
            short[] data = { ack };
            _device.BSend(PlcAddressMap.STS_ACK, 1, ref data);
        }

        /// <summary>
        /// 카메라 1대 결과를 D번지에 기록 (자재유무, Width, Height, Area, Done)
        /// </summary>
        private void WriteCamResult(MaterialCheckResult r)
        {
            if (r.CamNo < 1 || r.CamNo > PlcAddressMap.CAMERA_COUNT) return;

            string addr = PlcAddressMap.GetCamResultAddress(r.CamNo);
            short[] data =
            {
                r.Exist ? PlcAddressMap.RESULT_EXIST : PlcAddressMap.RESULT_NOT_EXIST,
                (short)r.Width,
                (short)r.Height,
                (short)(r.Area / 10.0),   // 픽셀² / 10 으로 정수화
                1                          // Done
            };
            _device.BSend(addr, data.Length, ref data);
        }

        private void ClearCamResult(int camNo)
        {
            if (camNo < 1 || camNo > PlcAddressMap.CAMERA_COUNT) return;
            string addr = PlcAddressMap.GetCamResultAddress(camNo);
            short[] data = { 0, 0, 0, 0, 0 };
            _device.BSend(addr, data.Length, ref data);
        }

        private void ClearAllResults()
        {
            for (int i = 1; i <= PlcAddressMap.CAMERA_COUNT; i++)
                ClearCamResult(i);
        }
        #endregion
    }
}
