using OpenQA.Selenium.Chrome;
using System.Threading;
using System.Windows;
using System.ServiceProcess;
using System.Security.Principal;

namespace OS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        Data data = new Data();
        Thread T0;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            if (T0 != null)
            {
                MessageBox.Show("Programm is running now");
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                data.Wait_Service[i].Reset();
     //           data.Wait_Service_Finish[i].Reset();
            }
            T0 = new Thread(Continue);
            T0.Start();
        }
        private void Button_Crush_Click(object sender, RoutedEventArgs e)
        {
            if (data.Multithread_planning)
            {
                label_Planning.Content = (data.Multithread_planning = !data.Multithread_planning).ToString();
                data.Wait_Read.Set();
                foreach (EventWaitHandle ewh in data.Wait_Service)
                    ewh.Set();
            }
            else
                MessageBox.Show("There is no turning back");
        }
       public void Continue()
        {
            ChromeOptions ChrOpt = new ChromeOptions();
            ChrOpt.AddArgument(@"user-data-dir=C:\Users\Дмитрий\AppData\Local\Google\Chrome\User Data\");
            data.chromeDriver = new ChromeDriver(ChrOpt);
            data.chromeDriver.Navigate().GoToUrl(@"https://vk.com/feed?section=recommended");
            data.FillData(data.chromeDriver);

            data.Threads[0] = new Thread(() => data.Write(ref data.Texts, "Texts.json", 0)) { Name = "Thread0" };
            data.Threads[1] = new Thread(() => data.Write(ref data.Images, "Images.json", 1)) { Name = "Thread1" };
            data.Threads[2] = new Thread(() => data.Write(ref data.Links, "Links.json", 2)) { Name = "Thread2" };
            data.Threads[3] = new Thread(data.ReadFiles) { Name = "Thread3" };

            for (int i = 0; i < data.Threads.Length; i++)
                data.Threads[i].Start();
        }
        private void Turn_Off(object sender, System.ComponentModel.CancelEventArgs e)
        {
            data.Threads_Are_Running = false;
            foreach (EventWaitHandle ewh in data.Wait_Write_Finish)
                ewh.Set();
            for (int k = 0; k < 3; k++)
                if (data.Threads[k] != null && data.Threads[k].IsAlive)
                {
                    data.Wait_Write[k].Set();
                    data.Threads[k].Join();
                }
            if (data.Threads[3] != null && data.Threads[3].IsAlive)
            {
                data.Wait_Read.Set();
                data.Threads[3].Join();
            }
            foreach (EventWaitHandle ewh in data.Wait_Service)
                ewh.Set();
            if (data.chromeDriver != null)
                data.chromeDriver.Dispose();
        }
        private void Button_Service_Click(object sender, RoutedEventArgs e)
        {
            WindowsIdentity wI = WindowsIdentity.GetCurrent();
            WindowsPrincipal wP = new WindowsPrincipal(wI);
            if (wP.IsInRole(WindowsBuiltInRole.Administrator))
            {
                ServiceController sc = new ServiceController("Service1");
                if (sc.Status.Equals(ServiceControllerStatus.Stopped))
                {
                    if (MessageBox.Show("Service is stopped. Do ypu want to run it?", "Change service state", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        sc.Start();
                        MessageBox.Show("Started!");
                    }
                }
                else
                    if (MessageBox.Show("Service is running. Do ypu want to stop it?", "Change service state", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        sc.Stop();
                        MessageBox.Show("Stopped!");
                    }
            }
            else
                MessageBox.Show("Run with administrator priveleges, please");
        }
    }
}