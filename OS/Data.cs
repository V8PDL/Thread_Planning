using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;

namespace OS
{

    class Data
    {
        public bool Multithread_planning, Threads_Are_Running;
        public List<IWebElement> webElements;
        public List<Text> Texts;
        public List<Image> Images;
        public List<Link> Links;
        public Thread[] Threads;
        public List<string> IDs;
        public int[] debug;
        public EventWaitHandle[] Wait_Write;
        public EventWaitHandle[] Wait_Write_Finish;
        public EventWaitHandle[] Wait_Service;
        public EventWaitHandle[] Wait_Service_Finish;
        public EventWaitHandle Wait_Read;
        public ChromeDriver chromeDriver;
        public class Text
        {
            public string ID;
            public string Post_Text;
            public Text(string ID, string Post_Text)
            {
                this.ID = ID;
                this.Post_Text = Post_Text;
            }
        }
        public class Image
        {
            public string ID;
            public string Image_URL;
            public Image(string ID, string Image_URL)
            {
                this.ID = ID;
                this.Image_URL = Image_URL;
            }
        }
        public class Link
        {
            public string ID;
            public string Link_URL;
            public Link(string ID, string Link_URL)
            {
                this.ID = ID;
                this.Link_URL = Link_URL;
            }
        }
        public Data()
        {
            webElements = new List<IWebElement>();
            Texts = new List<Text>();
            Images = new List<Image>();
            Links = new List<Link>();
            IDs = new List<string>();
            Threads = new Thread[4];
            Wait_Write = new EventWaitHandle[3];
            Wait_Service_Finish = new EventWaitHandle[3];
            Wait_Write_Finish = new EventWaitHandle[3];
            Wait_Service = new EventWaitHandle[3];

            for (int i = 0; i < Wait_Write.Length; i++)
            {
                Wait_Write[i] = new EventWaitHandle(true, EventResetMode.ManualReset, "Global\\Wait_Write" + i.ToString());
                Wait_Write_Finish[i] = new EventWaitHandle(false, EventResetMode.AutoReset, "Global\\Wait_Write_Finish" + i.ToString());

                if (!EventWaitHandle.TryOpenExisting("Global\\Wait_Service" + i.ToString(), out Wait_Service[i]))
                    Wait_Service[i] = new EventWaitHandle(false, EventResetMode.ManualReset, "Global\\Wait_Service" + i.ToString());

                if (!EventWaitHandle.TryOpenExisting("Global\\Wait_Service_Finish" + i.ToString(), out Wait_Service_Finish[i]))
                    Wait_Service_Finish[i] = new EventWaitHandle(true, EventResetMode.ManualReset, "Global\\Wait_Service_Finish" + i.ToString());
            }
            Wait_Read = new EventWaitHandle(false, EventResetMode.AutoReset, "Global\\Wait_Read");

            debug = new int[4];
            for (int i = 0; i < debug.Length; i++)
                debug[i] = 0;
            Multithread_planning = Threads_Are_Running = true;
        }
        public void Write<T>(ref List<T> list, string path, int N)
        {
            if (list.Any())
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(path))
                        sw.Write("[" + JsonConvert.SerializeObject(list[0]));
                }
                catch (System.IO.IOException e)
                {
                    MessageBox.Show("Exception! As expected. " + e.StackTrace);
                    foreach (EventWaitHandle ewh in Wait_Service)
                        ewh.Set();
                    throw;
                }
                list.RemoveAt(0);
            }
            else
            {
                MessageBox.Show("Something went wrong...");
                return;
            }
            for (int k = 0; Threads_Are_Running; k++)
            {
                if (Multithread_planning)
                    Wait_Write[N].WaitOne();

                if (k % 3 == N && Threads_Are_Running && Multithread_planning)
                {
                    if (k % 6 > 2)
                    {
                        Wait_Service[N].Set();
                        Wait_Service[N].Reset();
                        Wait_Service_Finish[N].WaitOne();
                    }
                    else
                        Wait_Read.Set();
                    Wait_Write_Finish[N].Set();
                    continue;
                }
                else
                {
                    if (k != 0 && k % 3 == (N + 1) % 3 && Threads_Are_Running && Multithread_planning)
                    {
                        WaitHandle.WaitAll(Wait_Write_Finish);
                        for (int i = 0; i < 3; i++)
                            Wait_Write[i].Set();
                    }
                }
                if (Multithread_planning)
                    Wait_Write[N].Reset();
                if (list.Any() && Threads_Are_Running)
                {
                    {
                        string ToWrite = ("," + JsonConvert.SerializeObject(list//.GetRange(count, list.Count - 1 - count)
                            ).Trim(']', '[', ','));
                        try
                        {
                            if (!ToWrite.Equals(","))
                                using (StreamWriter sw = new StreamWriter(path, true))
                                {
                                    sw.Write(ToWrite);
                                }
                        }
                        catch (IOException e)
                        {
                            MessageBox.Show("Exception! As expected. " + e.StackTrace);
                            foreach (EventWaitHandle ewh in Wait_Service)
                                ewh.Set();
                            throw;
                        }
                    }
                    list = new List<T>();
                }
                debug[N]++;
                Wait_Write_Finish[N].Set();
                if (k % 3 == (N + 1) % 3 && Threads_Are_Running)
                    FillData(chromeDriver);
            }
            MessageBox.Show("Thread" + N.ToString() + " is over");
        }
        public void ReadFiles()
        {
            string[] path = new string[3] { "Texts.json", "Images.json", "Links.json" };
            for (int i = 0; Threads_Are_Running; i++)
            {
                if (Multithread_planning)
                    Wait_Read.WaitOne();
                Wait_Write[i % 3].Reset();
                try
                {
                    using (StreamReader sr = new StreamReader(path[i % 3]))
                        JsonConvert.DeserializeObject(sr.ReadToEnd() + ']');
                }
                catch (System.IO.IOException e)
                {
                    MessageBox.Show("Exception! As expected. " + e.StackTrace);
                    foreach (EventWaitHandle ewh in Wait_Service)
                        ewh.Set();
                    throw;
                }
                debug[3]++;
                Wait_Write[i % 3].Set();
            }
            MessageBox.Show("Reading files is over");
        }
        public void FillData(ChromeDriver chromeDriver)
        {

            webElements = (from w in chromeDriver.FindElementsByClassName("wall_text") where w.Displayed 
                           //&&!IDs.Contains(w.FindElement(By.TagName("div")).GetAttribute("id").Remove(0, 4))
                           select w).ToList();
            chromeDriver.ExecuteScript("arguments[0].scrollIntoView();", webElements.Last());

            string ID;

            for (int i = IDs.Count; i < webElements.Count; i++)
            {
                IWebElement element = webElements[i];
                ID = element.FindElement(By.TagName("div")).GetAttribute("id");
                if (string.IsNullOrEmpty(ID))
                    continue;
                ID.Remove(0, 4);
                if (IDs.Contains(ID))
                    continue;
                IDs.Add(ID);

                if (!string.IsNullOrEmpty(element.Text))
                {
                    Texts.Add(new Text(ID, element.Text.Replace("\"", "\'")));
                }
                List<IWebElement> links = (from l in element.FindElements(By.TagName("a")) where l.Displayed 
                            //               && (!string.IsNullOrEmpty(l.GetAttribute("href")) || !string.IsNullOrEmpty(l.GetAttribute("onclick")))
                                           select l).ToList();
                if (links.Any())
                {
                    foreach (IWebElement link in links)
                    {
                        string href = link.GetAttribute("href");
                        string onclick = link.GetAttribute("onclick");
                        if (!string.IsNullOrEmpty(href))
                        {
                                Links.Add(new Link(ID, href));
                        }
                        else
                            if (!string.IsNullOrEmpty(onclick) && onclick.Contains(".jpg"))
                        {
                                Images.Add(new Image(ID, onclick.Substring(onclick.IndexOf("http"), onclick.IndexOf(".jpg") - onclick.IndexOf("http") + 4).Replace(@"\/", @"/")));
                            //Images.Add(new Image(ID, Regex.Match(link.GetAttribute("onclick"), @"https:*.jpg").Value.Replace(@"\/", @"/")));
                        }
                    }
                }

            }
        }
    }
}
