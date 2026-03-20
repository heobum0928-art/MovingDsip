
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using ReringProject.Define;
using ReringProject.Device;
using ReringProject.Sequence;
using ReringProject.Setting;
using ReringProject.Utility;

namespace ReringProject.UI {

    public enum MainViewMode {
        All,
        Original,
        Result,
    }

    public class MainViewModel : INotifyPropertyChanged {
        private string _SelectedSeqName;
        public string SelectedSeqName {
            get {
                return _SelectedSeqName;
            }
            set {
                _SelectedSeqName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedSeqName"));
            }
        }

        private int _SelectedSeqIndex;
        public int SelectedSeqIndex {
            get {
                return _SelectedSeqIndex;
            }

            set {
                _SelectedSeqIndex = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedSeqIndex"));
            }
        }

        private string _SelectedViewMode;
        public string SelectedViewMode {
            get {
                return _SelectedViewMode;
            }
            set {
                _SelectedViewMode = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedViewMode"));
            }
        }

        private int _SelectedViewIndex;
        public int SelectedViewIndex {
            get {
                return _SelectedViewIndex;
            }

            set {
                _SelectedViewIndex = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedViewIndex"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// MainView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainView : UserControl {
        private const string COMBOBOX_SEQUENCE_ALL = "All";
        MainWindow mParentWindow;
        DeviceHandler pDev;
        SequenceHandler pSeq;
        LightHandler pLight;
        
        private MemoryStream BackgroundImageStream = null;
        private object mDrawInterlock = new object();
        private Dictionary<string, SequenceContext> ContextList;
        private System.Windows.Point? lastMousePositionOnTarget;
        private System.Windows.Point? lastCenterPositionOnTarget;

        public int BackgroundWidth { get; private set; } = DeviceHandler.CORNER_ALIGN_CAMERA_WIDTH;
        public int BackgroundHeight { get; private set; } = DeviceHandler.CORNER_ALIGN_CAMERA_HEIGHT;

        private ScaleTransform ScaleTransform = new ScaleTransform();

        private Task GrabTask = null;
       
        private bool dragStarted = false;

        private MainViewModel Model;

        private List<IMainView> CustomViewList = new List<IMainView>();

        public bool IsEditable {
            get { return canvas_main.IsEditable; }
            set { canvas_main.IsEditable = value; }
        }

        public MainView() {
            InitializeComponent();

            canvas_main.RenderTransform = ScaleTransform;
            canvas_main._ScaleTransform = ScaleTransform;
            //image_foreground.RenderTransform = ScaleTransform;
            canvas_main.ParentScrollViewer = scrollViewer;
            Model = new MainViewModel();
            this.DataContext = Model;
        }
        
        public double DrawScale {
            get { return ScaleTransform.ScaleX; }
            set {
                dragStarted = true;
                
                ScaleTransform.ScaleX = value;
                ScaleTransform.ScaleY = value;
                
                canvas_main.Width = (BackgroundWidth * ScaleTransform.ScaleX);
                canvas_main.Height = (BackgroundHeight * ScaleTransform.ScaleY);
                
                slider_scale.Value = ScaleTransform.ScaleX * 100;
                
                //PrevPoint = pt;

                dragStarted = false;
            }
        }
        

        private void MainView_Loaded(object sender, RoutedEventArgs e) {
            mParentWindow = (MainWindow)System.Windows.Window.GetWindow(this);
            pDev = SystemHandler.Handle.Devices;
            pSeq = SystemHandler.Handle.Sequences;
            pLight = SystemHandler.Handle.Lights;

            ContextList = pSeq.GetContextDictionary();
            //set contextlist
            foreach (IMainView customView in CustomViewList) {
                customView.ContextList = ContextList;
            }

            //view mode
            comboBox_viewMode.Items.Clear();
            string[] names = Enum.GetNames(typeof(MainViewMode));
            for (int i = 0; i < names.Length; i++) {
                comboBox_viewMode.Items.Add(names[i].Replace("_", " "));
            }
            if (comboBox_viewMode.Items.Count > 0) comboBox_viewMode.SelectedIndex = 0;
            
            //sequence 
            comboBox_sequence.Items.Clear();
            comboBox_sequence.Items.Add(COMBOBOX_SEQUENCE_ALL);
            for(int i = 0; i < pSeq.Count; i++) {
                comboBox_sequence.Items.Add(pSeq[i].Name);
            }
            if (comboBox_sequence.Items.Count > 0) comboBox_sequence.SelectedIndex = 0;

            this.DrawScale = pDev.Config.DrawScale;
        }

        public void AddCustomControl(string name, UserControl control) {
            TabItem newItem = new TabItem();
            newItem.Header = name;
            newItem.Visibility = Visibility.Visible;
            newItem.Height = 42;
            newItem.Content = control;
            tabControl_view.Items.Add(newItem);
            if((control is IMainView) == false) {
                throw new Exception("Custom control is not IMainView type.");
            }
            IMainView mainView = control as IMainView;
            CustomViewList.Add(mainView);
        }

        public void ChangeTabPage(int index) {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => { 
                tabControl_view.SelectedIndex = index;
            }));
        }

        public void SetParam(ESequence seqID, ParamBase param) {
            if (pSeq[seqID] == null) return;

            string selectedSeq = pSeq[seqID].Name;
            Model.SelectedSeqIndex = comboBox_sequence.Items.IndexOf(COMBOBOX_SEQUENCE_ALL);
            if (Model.SelectedSeqName == COMBOBOX_SEQUENCE_ALL) {
                SequenceContext context = ContextList[selectedSeq];
                DisplayParam(context, param);
            }
        }

        private bool DisplayToBackground(Mat img) {
            try {
                //background
                if (img != null && (img.Empty() == false)) {
                    using (BackgroundImageStream = new MemoryStream()) {
                        img.WriteToStream(BackgroundImageStream, ".bmp");
                        BackgroundImageStream.Seek(0, SeekOrigin.Begin);
                        BitmapFrame frame = BitmapFrame.Create(BackgroundImageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        canvas_main.Background = new ImageBrush(frame);
                        
                        BackgroundWidth = (int)frame.Width;
                        BackgroundHeight = (int)frame.Height;

                        canvas_main.Width = (BackgroundWidth * DrawScale);
                        canvas_main.Height = (BackgroundHeight * DrawScale);
                    }
                }
                else {
                    canvas_main.Background = Brushes.Black;
                    return false;
                }
            }
            catch(Exception e) {
                Logging.PrintErrLog((int)ELogType.Error, e.Message);
                return false;
            }
            return true;
        }
        
        public async void GrabAndDisplay(ICameraParam param, bool eventCall = false) {
            if (param == null) return;
            if (pSeq.IsIdle == false) {
                return;
            }
            
            if (GrabTask != null) {
                return;
            }

            GrabTask = Task.Run(() => {
                lock (mDrawInterlock) {
                    //grab 수행
                    //pDev.ApplyProperty(param);
                    bool res = pLight.ApplyLight(param);
                    Mat grabbedImage = pDev.GrabImage(param);
                    param.PutImage(grabbedImage);

                    this.Dispatcher.BeginInvoke( System.Windows.Threading.DispatcherPriority.Normal, new Action(() => {
                        bool isGrabFromFile = pDev[param.DeviceName].IsGrabFromFile;
                        TimeSpan elapsed = pDev[param.DeviceName].ElapsedTime;
                        string stateMsg;
                        Brush br = new SolidColorBrush(Colors.Red);
                        canvas_main.SetParam(param);
                        if (pDev[param.DeviceName] == null) {
                            stateMsg = "Device Not Opened";
                            br = new SolidColorBrush(Colors.Red);
                            label_message.Foreground = br;
                        }
                        else if (DisplayToBackground(grabbedImage)) {
                            //update state
                            stateMsg = "Grab Success";
                            if (isGrabFromFile) stateMsg = "Grab From File";
                            br = new SolidColorBrush(Colors.Lime);
                            label_message.Foreground = br;
                        }
                        else {
                            stateMsg = "Grab Fail";
                            br = new SolidColorBrush(Colors.Red);
                            label_message.Foreground = br;
                        }
                        
                        string resultStr = string.Format("{0}\n{1} ({2:0.00}s)", param.DeviceName, stateMsg, ((double)elapsed.TotalMilliseconds / 1000.0f));
                        label_message.Content = resultStr;

                        foreach (IMainView customView in CustomViewList) {
                            customView.Display(param.SequenceName, grabbedImage, resultStr, br, param.ActionName);
                        }

                        canvas_main.InvalidateVisual();
                    }));
                }
            });
            await GrabTask;

            GrabTask.Dispose();
            GrabTask = null;
        }
        
        public void DisplayParam(SequenceContext context, ParamBase param) {
            lock (mDrawInterlock) {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => {
                    //background
                    DisplayToBackground(context.ResultImage);

                    canvas_main.SetParam(param);

                    //update state
                    string resultStr = string.Format("{0}\n{1} ({2:0.00}s)", param.ToString(), context.ResultString, ((double)context.Timer.Elapsed.TotalMilliseconds / 1000.0f));
                    label_message.Content = resultStr;
                    switch (context.Result) {
                        case EContextResult.None:
                            label_message.Foreground = new SolidColorBrush(Colors.Yellow);
                            break;
                        case EContextResult.Pass:
                            label_message.Foreground = new SolidColorBrush(Colors.Lime);
                            break;
                        case EContextResult.Fail:
                            label_message.Foreground = new SolidColorBrush(Colors.Red);
                            break;
                        case EContextResult.Error:
                            label_message.Foreground = new SolidColorBrush(Colors.Yellow);
                            break;
                    }

                    foreach (IMainView customView in CustomViewList) {
                        customView.Display(param.Parent.Name, context.ResultImage, resultStr, label_message.Foreground, param.OwnerName);
                    }

                    canvas_main.InvalidateVisual();
                }));
            }
        }
        
        //select comboBox
        public void DisplaySequenceContext(SequenceContext context) {
            //comboBox
            string selectedItem = Model.SelectedSeqName;
            if ((selectedItem != COMBOBOX_SEQUENCE_ALL) && (selectedItem != context.Source.Name)) {
                return;
            }

            lock (mDrawInterlock) {
                this.Dispatcher.BeginInvoke( System.Windows.Threading.DispatcherPriority.Normal,new Action(() => {
                    //background
                    DisplayToBackground(context.ResultImage);

                    canvas_main.SetContext(context);
                    string name = context.Source.Name;
                    if (context.ActionParam != null) name = context.ActionParam.ToString();
                    //update state
                    string resultStr = string.Format("{0}\n{1} ({2:0.00}s)", name, context.ResultString, ((double)context.Timer.Elapsed.TotalMilliseconds / 1000.0f));
                    label_message.Content = resultStr;
                    switch (context.Result) {
                        case EContextResult.None:
                            label_message.Foreground = new SolidColorBrush(Colors.Yellow);
                            break;
                        case EContextResult.Pass:
                            label_message.Foreground = new SolidColorBrush(Colors.Lime);
                            break;
                        case EContextResult.Fail:
                            label_message.Foreground = new SolidColorBrush(Colors.Red);
                            break;
                        case EContextResult.Error:
                            label_message.Foreground = new SolidColorBrush(Colors.Yellow);
                            break;
                    }

                    foreach (IMainView customView in CustomViewList) {
                        //customView.Display(context.Source.Name, context.ResultImage, resultStr, label_message.Foreground, context.ActionParam.OwnerName); // Origin Code
                        // 2024.01.04 수정 start
                        if (context.ActionParam != null)
                            customView.Display(context.Source.Name, context.ResultImage, resultStr, label_message.Foreground, context.ActionParam.OwnerName);
                        else
                            customView.Display(context.Source.Name, context.ResultImage, resultStr, label_message.Foreground);
                        // 2024.01.04 수정 End
                    }

                    canvas_main.InvalidateVisual();
                }));
            }
        }
        
        private void ComboBox_sequence_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Model.SelectedSeqIndex < 0) return;
            
            string selectedName = Model.SelectedSeqName;
            if (selectedName == COMBOBOX_SEQUENCE_ALL) return;

            SequenceContext context = ContextList[selectedName];
            DisplaySequenceContext(context);
        }

        private void ComboBox_viewMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {

        }
        
        private void Canvas_main_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {

            lastMousePositionOnTarget = Mouse.GetPosition(canvas_main);

            double zoom = e.Delta > 0 ? .05 : -.05;
            DrawScale += zoom;
            
            if (DrawScale <= 0.2) DrawScale = 0.2;
            else if (DrawScale > 2.0) DrawScale = 2.0;

            
            e.Handled = true;
        }
       

        private void Slider_DragCompleted(object sender, DragCompletedEventArgs e) {
            DrawScale = slider_scale.Value / 100;
            this.dragStarted = false;
        }

        private void Slider_DragStarted(object sender, DragStartedEventArgs e) {
            this.dragStarted = true;
        }

        private void Slider_scale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (this.IsLoaded == false) return;

            var centerOfViewport = new System.Windows.Point(scrollViewer.ViewportWidth / 2, scrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget = scrollViewer.TranslatePoint(centerOfViewport, canvas_main);

            
            if (!dragStarted) {
                DrawScale = slider_scale.Value / 100;
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0) {
                System.Windows.Point? targetBefore = null;
                System.Windows.Point? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue) {
                    if (lastCenterPositionOnTarget.HasValue) {
                        var centerOfViewport = new System.Windows.Point(scrollViewer.ViewportWidth / 2, scrollViewer.ViewportHeight / 2);
                        System.Windows.Point centerOfTargetNow = scrollViewer.TranslatePoint(centerOfViewport, canvas_main);

                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                }
                else {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(canvas_main);

                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue) {
                    double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                    double multiplicatorX = e.ExtentWidth / canvas_main.Width;
                    double multiplicatorY = e.ExtentHeight / canvas_main.Height;

                    double newOffsetX = scrollViewer.HorizontalOffset - dXInTargetPixels * multiplicatorX;
                    double newOffsetY = scrollViewer.VerticalOffset - dYInTargetPixels * multiplicatorY;

                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY)) {
                        return;
                    }

                    scrollViewer.ScrollToHorizontalOffset(newOffsetX);
                    scrollViewer.ScrollToVerticalOffset(newOffsetY);
                }
            }
        }
    }
}
