using CG.Web.MegaApiClient;
using CloudflareSolverRe.CaptchaProviders;
using CloudflareSolverRe.Types.Captcha;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace MegaTool
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public void addLog(string text)
        {
            using (StreamWriter sw = new StreamWriter("log.mgtool", true, System.Text.Encoding.Default))
            {
                sw.WriteLine("["+ DateTime.Now.ToString()+"]"+text);
            }
        }
        struct User
        {
            public string mail;
            public string pass;
            public User(string mail, string pass)
            {
                this.mail = mail;
                this.pass = pass;
            }
        }        
        struct Config
        {
            public string standartMail;
            public string pathToDownload;
            public string tempPassword;
            public Config(string standartMail, string pathToDownload, string tempPassword)
            {
                this.standartMail = standartMail;
                this.pathToDownload = pathToDownload;
                this.tempPassword = tempPassword;
            }
        }        
        struct ThreadDataWaitEmail
        {
            public string verificationText;
            public string Adress;
            public ThreadDataWaitEmail(string verificationText, string Adress)
            {
                this.verificationText = verificationText;
                this.Adress = Adress;
            }
        }
        MegaApiClient curentClient = new MegaApiClient();
        //MegaApiClient sendToClient = new MegaApiClient();
        List<User> user = new List<User>();
        Config config = new Config();
        string prevComboBoxText = "";
        Form2 form2;
        Form3 form3;
        Progress<double> progressMegaAction = new Progress<double>();
        Task<INode> curentTask;
        Task<Stream> curentTaskMove;
        string curentNameFileMove;
        string startRegProc(string arg)
        {
            var proc1 = new ProcessStartInfo()
            {
                UseShellExecute = false,
                //WorkingDirectory = @"C:\Windows\System32",
                FileName = @"MegaTools\megareg.exe",
                //Arguments = "/c " + cmd,
                Arguments = arg,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true
                //Verb = "runas"
            };
            
            Process tempProc = Process.Start(proc1);
            tempProc.WaitForExit();
            StreamReader tempStreamReader = tempProc.StandardOutput;
            return tempStreamReader.ReadToEnd();
        }
        string GenRandomStringMail()
        {
            string Alphabet = "abcdefghijklmnopqrstuvwxyz1234567890";
            Random rnd = new Random();
            StringBuilder sb = new StringBuilder(10);
            int Position = 0;
            
            for (int i = 0; i < 10; i++)
            {
                Position = rnd.Next(0, Alphabet.Length - 1);
                sb.Append(Alphabet[Position]);
            }
            return sb.ToString();
        }
        public void registrateAccount(string pass,string name)
        {
            //this.Enabled = false;
            try
            {
                //Console.WriteLine(pass);
                Random rnd = new Random();
                label2.Text = "Creating temp mail";
                progressBar1.Maximum = 4;
                progressBar1.Value = 0;
                var client = new RestClient("https://privatix-temp-mail-v1.p.rapidapi.com/request/domains/");
                var request = new RestRequest(Method.GET);
                request.AddHeader("x-rapidapi-key", "cbe5a1c099msh44b087c0a0696ffp16afcbjsn6ebd067a4213");
                request.AddHeader("x-rapidapi-host", "privatix-temp-mail-v1.p.rapidapi.com");
                IRestResponse response = client.Execute(request);
                List<string> domain = new List<string>();
                string tempLineDomains = response.Content;
                //MessageBox.Show(response.Content);
                while (tempLineDomains.IndexOf('"') > -1)
                {
                    tempLineDomains=tempLineDomains.Substring(tempLineDomains.IndexOf('"') + 1);
                    domain.Add(tempLineDomains.Substring(0, tempLineDomains.IndexOf('"')));
                    //MessageBox.Show(domain[domain.Count - 1]);
                    tempLineDomains = tempLineDomains.Substring(tempLineDomains.IndexOf('"') + 1);
                }
                
                string Address = GenRandomStringMail()+domain[rnd.Next(0, domain.Count - 1)];
                //Address = "toboped522@1981pc.com";


            //Console.WriteLine(Address);
                addLog("User email to register:" + Address);
                label2.Text = "Registrating in mega.nz";
                progressBar1.Value = 1;
                string verificationText = startRegProc("--register --email " + Address + " --name " + name + " --password " + pass);
                addLog("Get info from proc:" + verificationText);
                //MessageBox.Show(verificationText);
                if (verificationText == "")
            {
                registrateAccount( pass, name);

            }
            else
            {
                verificationText = verificationText.Substring(verificationText.IndexOf("megareg.exe") + 12);
                verificationText = verificationText.Substring(0, verificationText.IndexOf("@LINK@"));
                label2.Text = "Waiting email from mega.nz";
                progressBar1.Value = 2;
                Thread myThread = new Thread(waitEmailThread);
                myThread.Start(new ThreadDataWaitEmail(verificationText,Address));
                
                

            }


            }
            catch(Exception ex)
            {
                enableForm();
                timer1.Enabled = true;
                label2.Text = "Error:" + ex.Message + ex.Data;
                //this.Enabled = true;
                progressBar1.Value = 0;
            }
        }
        public void waitEmailThread(object threadDataWaitEmail)
        {
            while (true)
            {
                try
                {


                    ThreadDataWaitEmail threadDataWaitEmailTemp = (ThreadDataWaitEmail)threadDataWaitEmail;
                    string verificationText = threadDataWaitEmailTemp.verificationText;
                    string Address = threadDataWaitEmailTemp.Adress;

                    var client = new RestClient("https://privatix-temp-mail-v1.p.rapidapi.com/request/mail/id/" + MD5Hash(Address) + "/");
                    var request = new RestRequest(Method.GET);
                    request.AddHeader("x-rapidapi-key", "cbe5a1c099msh44b087c0a0696ffp16afcbjsn6ebd067a4213");
                    request.AddHeader("x-rapidapi-host", "privatix-temp-mail-v1.p.rapidapi.com");
                    IRestResponse response = client.Execute(request);
                    string responseText = response.Content;
                    //threadDataWaitEmail
                    //Console.WriteLine(htmlBody.Count);
                    if (responseText.IndexOf("mail_id") > 0)
                    {
                        addLog("Have a new mail:" + responseText);
                        string mailIdTemp = responseText.Substring(responseText.IndexOf("mail_id") + 10);
                        mailIdTemp = mailIdTemp.Substring(0, mailIdTemp.IndexOf('"'));
                        addLog("Get MAIL ID:" + mailIdTemp);

                        client = new RestClient("https://privatix-temp-mail-v1.p.rapidapi.com/request/one_mail/id/"+ mailIdTemp + "/");
                        request = new RestRequest(Method.GET);
                        request.AddHeader("x-rapidapi-key", "cbe5a1c099msh44b087c0a0696ffp16afcbjsn6ebd067a4213");
                        request.AddHeader("x-rapidapi-host", "privatix-temp-mail-v1.p.rapidapi.com");
                        response = client.Execute(request);
                        addLog("Email Text:" + responseText);

                        responseText = response.Content;
                        if (responseText.IndexOf(Address) > -1)
                        {
                            string verificationLink = responseText;

                            //MessageBox.Show(verificationLink);
                            verificationLink = verificationLink.Substring(verificationLink.IndexOf("https://mega.nz/#confirm"));
                            //MessageBox.Show(verificationLink);
                            verificationLink = verificationLink.Substring(0, verificationLink.IndexOf('\"')-1);
                            //MessageBox.Show(verificationLink);
                            addLog("Get verefication link:" + verificationLink);
                            verificationText = verificationText + verificationLink;
                            Invoke(new Action(() =>
                            {
                                label2.Text = "Accepting email";
                                progressBar1.Value = 3;
                            }));

                            bool successAcceptEmail = false;
                            //Console.WriteLine(procInfo);
                            for (int i = 0; i < 4; i++)
                            {
                                string procInfo = startRegProc(verificationText);
                                addLog("Get info from second proc:" + procInfo);
                                if (procInfo.IndexOf("Account registered successfully!") > -1)
                                {
                                    //Console.WriteLine("qwer");
                                    using (StreamWriter sw = new StreamWriter("user.mgtool", true, System.Text.Encoding.Default))
                                    {
                                        successAcceptEmail = true;
                                        sw.WriteLine(Address + " " + config.tempPassword);
                                    }
                                    break;
                                }
                            }

                            Invoke(new Action(() =>
                            {
                                enableForm();
                                label2.Text = "Reload users";
                                progressBar1.Value = 4;
                                loadUsers();
                                if (successAcceptEmail)
                                {

                                    progressBar1.Value = 0;
                                    label2.Text = "";
                                }
                                else
                                {
                                    timer1.Enabled = true;
                                    label2.Text = "Error temp Email system, try again" ;
                                    //this.Enabled = true;
                                    progressBar1.Value = 0;
                                }

                            }));


                            break;
                        }

                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
            catch (Exception ex)
            {
                    Invoke(new Action(() =>
                    {
                        enableForm();
                        timer1.Enabled = true;
                        label2.Text = "Error:" + ex.Message + ex.Data;
                        //this.Enabled = true;
                        progressBar1.Value = 0;
                    }));

            }
            
                
                    //MessageBox.Show(htmlBody[0]);

            }
           
           
        }
        private void button1_Click(object sender, EventArgs e)
        {

        }
        void getUrlRecursive(IEnumerable<INode> nodes, INode parent, string fileName, int level = 0)
        {
            IEnumerable<INode> children = nodes.Where(x => x.ParentId == parent.Id);
            foreach (INode child in children)
            {

                if (child.Name== fileName)
                {
                    Uri tempUri=curentClient.GetDownloadLink(child);
                    string tempURL = tempUri.ToString();
                    Clipboard.SetText(tempURL);
                    MessageBox.Show("URL copied to clipboard", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                }
                //string infos = $"- {child.Name} - {child.Size/1000000} bytes - {child.CreationDate}";
                //Console.WriteLine(infos.PadLeft(infos.Length + level, '\t'));
                if (child.Type == NodeType.Directory)
                {
                    getUrlRecursive(nodes, child, fileName, level + 1);
                }
            }
        }         
        void getUrlToSendFileRecursive(IEnumerable<INode> nodes, INode parent, string fileName,string login, string pass, int level = 0)
        {
            IEnumerable<INode> children = nodes.Where(x => x.ParentId == parent.Id);
            foreach (INode child in children)
            {

                if (child.Name== fileName)
                {
                    int timeToLoad = 5000;
                    //IEnumerable<INode> nodesTemp = curentClient.GetNodes();
                    //INode root = nodesTemp.Single(n => n.Type == NodeType.Root);
                    //INode tempNode = curentClient.Move(child, root);

                    Uri tempUri=curentClient.GetDownloadLink(child);
                    Console.WriteLine(tempUri.ToString());
                    //MessageBox.Show(child.Type.ToString() + "QQ" + tempUri.ToString());
                    homeURL = "https://mega.nz/login";
                    //ChromeOptions chromeOptions = new ChromeOptions();
                    //chromeOptions.AddArguments("headless");
                    //driver = new ChromeDriver(chromeOptions);
                    driver = new ChromeDriver();
                    driver.Navigate().GoToUrl(homeURL);
                    WebDriverWait wait = new WebDriverWait(driver, System.TimeSpan.FromSeconds(60));
                    wait.Until(driver => driver.FindElement(By.CssSelector("#login-name2")));
                    IWebElement element = driver.FindElement(By.CssSelector("#login-name2"));
                    Console.WriteLine("wait");
                    Thread.Sleep(timeToLoad);
                    Console.WriteLine("end");
                    element.SendKeys(login);
                    element = driver.FindElement(By.CssSelector("#login-password2"));
                    element.SendKeys(pass);
                    element = driver.FindElement(By.CssSelector(".big-red-button"));
                    
                    element.Click();
                    wait.Until(driver => driver.FindElement(By.CssSelector(".nw-fm-left-icons-panel")));                    
                    driver.Navigate().GoToUrl(tempUri.ToString());



                    //Thread.Sleep(5000);
                    wait = new WebDriverWait(driver, System.TimeSpan.FromSeconds(60));
                    //div.fm-dialog-close
                    IWebElement buttonImport =null;
                    
                    //div.button.link-button.right.fm-import-to-cloudrive
                    if (child.Type == NodeType.Directory)
                    {
                        //MessageBox.Show("qq");
                        wait.Until(driver => driver.FindElement(By.CssSelector("div.button.link-button.right.fm-import-to-cloudrive")));
                        buttonImport = driver.FindElement(By.CssSelector("div.button.link-button.right.fm-import-to-cloudrive"));
                    }
                    else
                    {
                        wait.Until(driver => driver.FindElement(By.CssSelector("div.download.big-button.button.to-clouddrive.transition")));
                        buttonImport = driver.FindElement(By.CssSelector("div.download.big-button.button.to-clouddrive.transition"));
                    }
                    //MessageBox.Show("qwe");
                    //Console.WriteLine("wait");
                    Thread.Sleep(timeToLoad);
                    buttonImport.Click();
                    //Console.WriteLine("end");

                    /*wait = new WebDriverWait(driver, System.TimeSpan.FromSeconds(60));
                    wait.Until(driver => driver.FindElement(By.CssSelector("div.fm-dialog-close")));
                    element = driver.FindElement(By.CssSelector("div.fm-dialog-close"));
                    //Console.WriteLine("wait");
                    Thread.Sleep(timeToLoad);
                    //Console.WriteLine("end");
                    element.Click();*/
                    wait = new WebDriverWait(driver, System.TimeSpan.FromSeconds(60));
                    wait.Until(driver => driver.FindElement(By.CssSelector("div.default-light-green-button.right.dialog-picker-button.active")));
                    buttonImport = driver.FindElement(By.CssSelector("div.default-light-green-button.right.dialog-picker-button.active"));
                    //Console.WriteLine("wait");
                    Thread.Sleep(timeToLoad);
                    //Console.WriteLine("end");
                    buttonImport.Click();
                    Thread.Sleep(timeToLoad);
                    driver.Close();
                    //curentClient.Move(tempNode, parent);
                    timer1.Enabled = true;
                    label2.Text = "Complete";
                    /*System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> imagesWithAlt = driver.FindElements(By.CssSelector("div[download.big-button.button.to-clouddrive.transition]"));
                    for(int i=0;i< imagesWithAlt.Count(); i++)
                    {
                        MessageBox.Show(imagesWithAlt[i].Text);
                        
                        imagesWithAlt[i].Click();
                    }*/

                    //driver.Close();
                    //wait = new WebDriverWait(driver, System.TimeSpan.FromSeconds(15));
                    //wait.Until(driver => driver.FindElement(By.CssSelector(".download.big-button.button.to-clouddrive.transition")));

                    break;
                }
                //string infos = $"- {child.Name} - {child.Size/1000000} bytes - {child.CreationDate}";
                //Console.WriteLine(infos.PadLeft(infos.Length + level, '\t'));
                if (child.Type == NodeType.Directory)
                {
                    getUrlToSendFileRecursive(nodes, child, fileName,login,pass, level + 1);
                }
            }
        }        
        void downloadRecursive(IEnumerable<INode> nodes, INode parent, string fileName, int level = 0)
        {
            IEnumerable<INode> children = nodes.Where(x => x.ParentId == parent.Id);
            foreach (INode child in children)
            {

                if (child.Name== fileName)
                {
                    curentClient.DownloadFileAsync(child, config.pathToDownload+ child.Name, progressMegaAction);
                    timer4.Enabled = true;
                    disableForm();
                    label2.Text = "File is downloading, please wait...";
                    break;
                }
                //string infos = $"- {child.Name} - {child.Size/1000000} bytes - {child.CreationDate}";
                //Console.WriteLine(infos.PadLeft(infos.Length + level, '\t'));
                if (child.Type == NodeType.Directory)
                {
                    downloadRecursive(nodes, child, fileName, level + 1);
                }
            }
        }        
        void deleteRecursive(IEnumerable<INode> nodes, INode parent, string fileName, int level = 0)
        {
            IEnumerable<INode> children = nodes.Where(x => x.ParentId == parent.Id);
            foreach (INode child in children)
            {

                if (child.Name== fileName)
                {
                    curentClient.Delete(child);

                    break;
                }
                //string infos = $"- {child.Name} - {child.Size/1000000} bytes - {child.CreationDate}";
                //Console.WriteLine(infos.PadLeft(infos.Length + level, '\t'));
                if (child.Type == NodeType.Directory)
                {
                    deleteRecursive(nodes, child, fileName, level + 1);
                }
            }
        }
        /*void sendFileRecursive(IEnumerable<INode> nodes, INode parent, string fileName, int level = 0)
        {
            IEnumerable<INode> children = nodes.Where(x => x.ParentId == parent.Id);
            foreach (INode child in children)
            {

                if (child.Name == fileName)
                {
                        

                    //sendToClient.Upload(curentClient.Download(child), fileName, root);
                    curentTaskMove = curentClient.DownloadAsync(child,progressMegaAction);
                    curentNameFileMove = fileName;
                    timer5.Enabled = true;
                    disableForm();
                    label2.Text = "File is downloading, please wait...";
                    break;
                }
                //string infos = $"- {child.Name} - {child.Size/1000000} bytes - {child.CreationDate}";
                //Console.WriteLine(infos.PadLeft(infos.Length + level, '\t'));
                if (child.Type == NodeType.Directory)
                {
                    sendFileRecursive(nodes, child, fileName, level + 1);
                }
            }
        }*/

        void DisplayNodesRecursive(IEnumerable<INode> nodes, INode parent, int level = 0)
        {
            IEnumerable<INode> children = nodes.Where(x => x.ParentId == parent.Id);
            foreach (INode child in children)
            {
                
                dataGridView1.Rows.Add(child.Name, (Math.Round(child.Size*1.0 / 1000000,2)).ToString() + " MB", child.CreationDate,child.Owner);
                //string infos = $"- {child.Name} - {child.Size/1000000} bytes - {child.CreationDate}";
                //Console.WriteLine(infos.PadLeft(infos.Length + level, '\t'));
                if (child.Type == NodeType.Directory)
                {
                    DisplayNodesRecursive(nodes, child, level + 1);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }
        public void loadUsers()
        {
            user.Clear();
            comboBox1.Items.Clear();
            comboBox2.Items.Clear();
            if (File.Exists("user.mgtool"))
                using (StreamReader sr = new StreamReader("user.mgtool", System.Text.Encoding.Default))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string mail = line.Substring(0, line.IndexOf(" "));
                        string pass = line.Substring(line.IndexOf(" ") + 1);
                        user.Add(new User(mail, pass));
                        comboBox1.Items.Add(mail);
                        comboBox2.Items.Add(mail);


                        //Console.WriteLine(line);
                    }
                }
            comboBox1.Items.Add("Settings");
            comboBox1.Items.Add("Add random...");
        }
        public void loadConfig()
        {
            if (File.Exists("config.mgtool"))
            {
                using (StreamReader sr = new StreamReader("config.mgtool", System.Text.Encoding.Default))
                {
                    string line = sr.ReadLine();
                    config.standartMail = line;
                    line = sr.ReadLine();
                    config.pathToDownload = line;                    
                    line = sr.ReadLine();
                    config.tempPassword = line;
                }
            }
            else
            {
                config.pathToDownload = "";
                if (user.Count > 0)
                {
                    config.standartMail = user[0].mail;
                }
                else
                {
                    config.standartMail = "";
                }
                
                config.tempPassword = "tempPass1010";
            }

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            loadUsers();
            loadConfig();
            
            progressMegaAction.ProgressChanged += (s, progressValue) =>
            {
                Invoke(new Action(() =>
                {
                    progressBar1.Maximum = 100;
                    //Update the UI (or whatever) with the progressValue 
                    progressBar1.Value = Convert.ToInt32(progressValue);
                }));

            };


            /*if (Mail == null)
            {
                Mail = new TempMail
                {
                    Proxy = new ProxyClient("195.208.172.70", 8080, ProxyType.Http)
                };
                Mail.NewMessage += Mail_NewMessage;
            }*/
            dataGridView1.MultiSelect = false;
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            label2.Text = "";
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (prevComboBoxText != comboBox1.Text)
            {
                prevComboBoxText = comboBox1.Text;
                if (comboBox1.Text == "Settings")
                {
                    List<string> tempMails = new List<string>();
                    List<string> tempPass = new List<string>();
                    for (int i = 0; i < user.Count; i++)
                    {
                        tempMails.Add(user[i].mail);
                        tempPass.Add(user[i].pass);
                    }
                    form3 = new Form3(tempMails, tempPass);
                    form3.Show();
                    disableForm();
                    timer3.Enabled = true;
                    //comboBox1.Text = config.standartMail;
                }
                else if (comboBox1.Text == "Add random...")
                {
                    disableForm();
                    registrateAccount(config.tempPassword, "Oliver");
                    comboBox1.Text = config.standartMail;
                }
                else
                {
                    for (int i = 0; i < user.Count(); i++)
                    {
                        if (user[i].mail == comboBox1.Text)
                        {
                            if (curentClient.IsLoggedIn)
                            {
                                curentClient.Logout();
                            }
                            try
                            {
                                curentClient.Login(user[i].mail, user[i].pass);
                            }
                            catch (Exception ex)
                            {
                                timer1.Enabled = true;
                                label2.Text = "Error wile login:" + ex.Message + ex.Data;
                                //this.Enabled = true;
                                progressBar1.Value = 0;
                            }

                            refreshFiles();



                        }
                    }
                }
            }
            

        }
        public void refreshFiles()
        {
            dataGridView1.Rows.Clear();
            if (curentClient.IsLoggedIn)
            {
                IEnumerable<INode> nodes = curentClient.GetNodes();

                INode parent = nodes.Single(n => n.Type == NodeType.Root);
                DisplayNodesRecursive(nodes, parent);
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (curentClient.IsLoggedIn)
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    InitialDirectory = Environment.CurrentDirectory,
                    Title = "Choose file to upload",

                    CheckFileExists = true,
                    CheckPathExists = true,

                    //DefaultExt = "txt",
                    Filter = "All files (*.*)|*.*",
                    FilterIndex = 2,
                    RestoreDirectory = true,

                    ReadOnlyChecked = true,
                    ShowReadOnly = true
                };

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    IEnumerable<INode> nodes = curentClient.GetNodes();
                    INode root = nodes.Single(x => x.Type == NodeType.Root);
                    curentTask = curentClient.UploadFileAsync(openFileDialog1.FileName, root, progressMegaAction);
                    timer4.Enabled = true;
                    disableForm();
                    label2.Text = "File is uploading, please wait...";




                    
                }
            }


        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (curentClient.IsLoggedIn)
            {
                Int32 selectedCellCount =
                    dataGridView1.GetCellCount(DataGridViewElementStates.Selected);
                if (selectedCellCount > 0)
                {
                    string fileNameToFind = dataGridView1.Rows[dataGridView1.SelectedCells[0].RowIndex].Cells[0].Value.ToString();
                    IEnumerable<INode> nodes = curentClient.GetNodes();

                    INode parent = nodes.Single(n => n.Type == NodeType.Root);
                    getUrlRecursive(nodes, parent, fileNameToFind);
                    
                }
            }
            //MessageBox.Show(dataGridView1.ce.ToString());
                //curentClient.GetDownloadLink(myFile);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (curentClient.IsLoggedIn)
            {
                Int32 selectedCellCount =
                    dataGridView1.GetCellCount(DataGridViewElementStates.Selected);
                if (selectedCellCount > 0)
                {
                    string fileNameToFind = dataGridView1.Rows[dataGridView1.SelectedCells[0].RowIndex].Cells[0].Value.ToString();
                    IEnumerable<INode> nodes = curentClient.GetNodes();

                    INode parent = nodes.Single(n => n.Type == NodeType.Root);
                    downloadRecursive(nodes, parent, fileNameToFind);

                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            List<string> tempUsers = new List<string>();
            tempUsers.Add(config.standartMail);
            for (int i = 0; i < user.Count; i++)
            {
                tempUsers.Add(user[i].mail);
            }
            form2 = new Form2(tempUsers, config.pathToDownload, config.tempPassword);
            form2.Show();
            disableForm();
            timer2.Enabled = true;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            comboBox1.Text = config.standartMail;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (form2 != null)
            {
                if (!form2.CanSelect)
                {
                    timer2.Enabled = false;
                    loadConfig();
                    enableForm();
                }
            }

        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            if ((!form3.CanSelect) && (!form3.CanSelect))
            {
                timer3.Enabled = false;
                loadUsers();
                comboBox1.Text = config.standartMail;
                enableForm();

            }
        }

        public static string MD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }
        private IWebDriver driver;
        public string homeURL;
        private void button5_Click(object sender, EventArgs e)
        {

            //Assert.AreEqual("Sign In", element.GetAttribute("text"));

            for (int i = 0; i < user.Count(); i++)
            {
                if (user[i].mail == comboBox2.Text)
                {
                    /*if (sendToClient.IsLoggedIn)
                    {
                        sendToClient.Logout();
                    }
                    try
                    {
                        sendToClient.Login(user[i].mail, user[i].pass);
                    }
                    catch (Exception ex)
                    {
                        timer1.Enabled = true;
                        label2.Text = "Error wile login second account:" + ex.Message + ex.Data;
                        //this.Enabled = true;
                        progressBar1.Value = 0;
                    }*/

                    try{
                        if (curentClient.IsLoggedIn)
                        {
                            Int32 selectedCellCount =
                                dataGridView1.GetCellCount(DataGridViewElementStates.Selected);
                            if (selectedCellCount > 0)
                            {
                                string fileNameToFind = dataGridView1.Rows[dataGridView1.SelectedCells[0].RowIndex].Cells[0].Value.ToString();
                                IEnumerable<INode> nodes = curentClient.GetNodes();
                                INode parent = nodes.Single(n => n.Type == NodeType.Root);
                                //sendFileRecursive(nodes, parent, fileNameToFind);
                                getUrlToSendFileRecursive(nodes, parent, fileNameToFind, user[i].mail, user[i].pass);
                                //sendToClient.Logout();
                            }


                            //driver.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        timer1.Enabled = true;
                        label2.Text = "Error wile move:" + ex.Message + ex.Data;
                        //this.Enabled = true;
                        progressBar1.Value = 0;
                    }



                }
            }




        }
        public void disableForm()
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            comboBox1.Enabled = false;
            dataGridView1.Enabled = false;
        }        
        public void enableForm()
        {
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            comboBox1.Enabled = true;
            dataGridView1.Enabled = true;
        }
        private void timer4_Tick(object sender, EventArgs e)
        {
            if (curentTask.IsCompleted)
            {
                timer4.Enabled = false;
                enableForm();
                progressBar1.Value = 0;
                timer1.Enabled = true;
                label2.Text = "Complete";
                /*if (sendToClient.IsLoggedIn)
                {
                    sendToClient.Logout();
                }*/
                refreshFiles();
            }
        }

        private void timer5_Tick(object sender, EventArgs e)
        {
            /*if (curentTaskMove.IsCompleted)
            {
                timer5.Enabled = false;
                //MessageBox.Show(curentTaskMove.Result.Length.ToString() + "Q" + curentNameFileMove+"W"+user[0].mail);
                
                progressBar1.Value = 0;
                IEnumerable<INode> nodesTemp = sendToClient.GetNodes();
                INode root = nodesTemp.Single(x => x.Type == NodeType.Root);
                curentTask = sendToClient.UploadAsync(curentTaskMove.Result, curentNameFileMove, root, progressMegaAction);
                timer4.Enabled = true;
                label2.Text = "File is uploading, please wait...";
                
            }*/
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (curentClient.IsLoggedIn)
            {
                Int32 selectedCellCount =
                    dataGridView1.GetCellCount(DataGridViewElementStates.Selected);
                if (selectedCellCount > 0)
                {
                    string fileNameToFind = dataGridView1.Rows[dataGridView1.SelectedCells[0].RowIndex].Cells[0].Value.ToString();
                    IEnumerable<INode> nodes = curentClient.GetNodes();

                    INode parent = nodes.Single(n => n.Type == NodeType.Root);
                    deleteRecursive(nodes, parent, fileNameToFind);
                    refreshFiles();

                }
            }
        }
    }
}
