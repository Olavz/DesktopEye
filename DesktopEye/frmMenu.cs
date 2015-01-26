using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shell;

namespace DesktopEye
{
    public partial class frmMenu : Form
    {

        [DllImport("user32.dll")]
        static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        protected delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        protected static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        protected static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")]
        protected static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")]
        protected static extern bool IsWindowVisible(IntPtr hWnd);


        public static uint GetWindowProcessId(IntPtr hWnd)
        {
            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            return pid;
        }


        


        #region Eye tracking variables
        private System.Net.Sockets.TcpClient socket;
        private System.Threading.Thread incomingThread;
        private System.Timers.Timer timerHeartbeat;
        private Boolean isRunning;
        #endregion

        private int mouseX, mouseY = 0;

        private ArrayList listWindowInfo;
        private ArrayList listItemViews;
        private int screenWidth;
        private int screenHeight;
        private bool formVisible = false;

        private frmTriggerArea fta;

        private Process selectedProcess;

        public frmMenu()
        {
            InitializeComponent();
            listWindowInfo = new ArrayList();
            listItemViews = new ArrayList();

            Connect();
            
        }

        private void getWindowsAndBuild()
        {
            // Clear lists
            listItemViews.Clear();
            listWindowInfo.Clear();


            this.Controls.Clear();

            // Iterate windows
            EnumWindows(new EnumWindowsProc(EnumTheWindows), IntPtr.Zero);

            // Build controlls
            buildControlls(listWindowInfo);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            fta = new frmTriggerArea();
            screenWidth = Screen.PrimaryScreen.Bounds.Width;
            screenHeight = Screen.PrimaryScreen.Bounds.Height;

            this.Height = screenHeight;
            this.Width = screenWidth;
            this.Top = 0;
            this.Left = 0;

            setFormVisability(true);


        }

        private void buildControlls(ArrayList listWindowInfo)
        {

            for (int i = 0; i < listWindowInfo.Count; i++)
            {
                WindowInfo wi = (WindowInfo)listWindowInfo[i];

                ItemView iv = new ItemView();
                iv.setLabel(wi.name);
                iv.setImage(wi.image);
                iv.setProcess(wi.process);
                iv.Top = screenHeight - iv.Height - 5;
                iv.Left = i * (iv.Width + 5) + 5;
                iv.setCallback(this);
                this.Controls.Add(iv);
                listItemViews.Add(iv);
            }
        }

        public void updateSelectedProcess(Process p)
        {
            this.selectedProcess = p;
        }

        private bool EnumTheWindows(IntPtr hWnd, IntPtr lParam)
        {
            int size = GetWindowTextLength(hWnd);
            if (size++ > 0 && IsWindowVisible(hWnd))
            {
                WindowInfo wi = new WindowInfo();

                StringBuilder sb = new StringBuilder(size);
                GetWindowText(hWnd, sb, size);
                wi.name = sb.ToString();

                uint id = GetWindowProcessId(hWnd);
                Process p = Process.GetProcessById((int)id);
                wi.process = p;

                Icon ico = Icon.ExtractAssociatedIcon(p.MainModule.FileName);
                wi.image = ico.ToBitmap();

                /* 
                 * Dont want to show process for explorer 
                 * or DesktopEye, so we skip them. 
                 */ 
                switch (wi.process.ProcessName)
                {
                    case "explorer":
                        return true;
                    case "DesktopEye.vshost":
                        return true;
                    case "DesktopEye":
                        return true;
                    case "EyeTribe":
                        return true;
                }

                listWindowInfo.Add(wi);
            }
            return true;
        }


        public bool Connect()
        {
            Console.WriteLine("start connect");
            try
            {
                socket = new System.Net.Sockets.TcpClient("localhost", 6555);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error connecting: " + ex.Message);
                return false;
            }

            // Send the obligatory connect request message
            string REQ_CONNECT = "{\"values\":{\"push\":true,\"version\":1},\"category\":\"tracker\",\"request\":\"set\"}";
            Send(REQ_CONNECT);

            // Lauch a seperate thread to parse incoming data
            incomingThread = new System.Threading.Thread(ListenerLoop);
            incomingThread.Start();

            // Start a timer that sends a heartbeat every 250ms.
            // The minimum interval required by the server can be read out 
            // in the response to the initial connect request.   

            string REQ_HEATBEAT = "{\"category\":\"heartbeat\",\"request\":null}";
            timerHeartbeat = new System.Timers.Timer(300);
            timerHeartbeat.Elapsed += delegate { Send(REQ_HEATBEAT); };
            timerHeartbeat.Start();

            return true;
        }

        private void Send(string message)
        {
            if (socket != null && socket.Connected)
            {
                StreamWriter writer = new StreamWriter(socket.GetStream());
                writer.WriteLine(message);
                writer.Flush();
            }
        }


        public event EventHandler<ReceivedDataEventArgs> OnData;

        private void ListenerLoop()
        {
            StreamReader reader = new StreamReader(socket.GetStream());
            isRunning = true;

            while (isRunning)
            {
                string response = string.Empty;

                try
                {
                    response = reader.ReadLine();
                    JObject jObject = JObject.Parse(response);

                    Packet p = new Packet();
                    p.RawData = response;

                    p.Category = (string)jObject["category"];
                    p.Request = (string)jObject["request"];
                    p.StatusCode = (string)jObject["statuscode"];

                    JToken values = jObject.GetValue("values");


                    if (values != null)
                    {
                        // sanitation
                        p.Values = values.ToString().Replace("\r\n", "");

                        /* 
                          We can further parse the Key-Value pairs from the values here.
                          For example using a switch on the Category and/or Request 
                          to create Gaze Data or CalibrationResult objects and pass these 
                          via separate events.
                        */
                        JObject frame = values["frame"].Value<JObject>();
                        JObject avg = frame["avg"].Value<JObject>();
                        int xOff = (int)avg.GetValue("x");
                        int yOff = (int)avg.GetValue("y");

                        if (tmrGetMousePosition.Enabled)
                        {
                            xOff = mouseX;
                            yOff = mouseY;
                        }

                        if (formVisible)
                        {

                            // Hide form if eyes exits on the y-axis.
                            if (yOff < (screenHeight-200))
                            {
                                if (InvokeRequired)
                                {
                                    this.Invoke(new Action(() => setFormVisability(false)));
                                }
                            }

                            // Iterate applications and give focus as we look at them.
                            for (int i = 0; i < listItemViews.Count; i++ )
                            {
                                ItemView iv = (ItemView)listItemViews[i];
                                if (xOff > iv.Left && xOff < iv.Left + iv.Width)
                                {
                                    if (yOff > iv.Top && yOff < iv.Top + iv.Height)
                                    {
                                    
                                        if (InvokeRequired)
                                        {
                                            this.Invoke(new Action(() => setAppFocusFromIndex(i)));
                                        }
                                    }
                                }
                            }

                        }
                        else
                        {
                            // Looking down in the left corner will trigger the menu to show.
                            if (xOff < 200 && yOff > screenHeight-200)
                            {
                                if (InvokeRequired)
                                {
                                    this.Invoke(new Action(() => setFormVisability(true)));
                                }
                            }
                        }

                    }

                    // Raise event with the data
                    if (OnData != null)
                    {
                        OnData(this, new ReceivedDataEventArgs(p));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while reading response: " + ex.Message);
                }
            }
        }

        private void setAppFocusFromIndex(int index)
        {
            for (int i = 0; i < listItemViews.Count; i++)
            {
                ((ItemView)listItemViews[i]).setAppFocus(false);
            }
            ((ItemView)listItemViews[index]).setAppFocus(true);
        }

        private void setFormVisability(bool visible)
        {

            if (visible)
            {
                // Need to find active windows and build menu before we show.
                getWindowsAndBuild();
                this.Opacity = 1;
                formVisible = true;
                fta.Hide();
            }
            else
            {
                // Move selected application to top on exit.
                if (selectedProcess != null)
                {
                    // TODO: Should detect window state and not force windowstate as normal.
                    // http://msdn.microsoft.com/en-us/library/windows/desktop/ms633548(v=vs.85).aspx
                    ShowWindow(selectedProcess.MainWindowHandle, 3);
                    SetForegroundWindow(selectedProcess.MainWindowHandle);
                }


                this.Opacity = 0;
                formVisible = false;
                fta.Show();
            }
            
        }

        private void tmrGetMousePosition_Tick(object sender, EventArgs e)
        {
            Point p = MousePositionInfo.GetCursorPosition();
            mouseX = p.X;
            mouseY = p.Y;
        }

        
    }
}
