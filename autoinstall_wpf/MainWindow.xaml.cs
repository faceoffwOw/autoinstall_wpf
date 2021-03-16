using CredentialManagement;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Management;
using System.DirectoryServices;
using System.Security.Principal;
using System.Threading;
using System.Net.NetworkInformation;
using System.DirectoryServices.AccountManagement;

namespace autoinstall_wpf
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static List<Data> resultCheckbox = new List<Data>();
        static List<Data> lnkCheckbox = new List<Data>();
        static List<string> pcname = new List<string>(); //получаем список компьютеров в AD
        static List<string> lnk = new List<string>(); //получаем список ярлыков на рабочем столе пользователя
        static List<string> Public_lnk = new List<string>(); //получаем список ярлыков на общем рабочем столе
        List<string> programs = new List<string>();
        Dictionary<string, string> userD = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();
            initWebNavInstall();
            initNameWebNav();
            initLnk();
            Thread myThread = new Thread(new ThreadStart(GetPCName));// запускаем метод в новом потоке
            myThread.Start(); // запускаем поток
            Canvas1.Visibility = Visibility.Visible;
            Canvas1.Opacity = 100;
            Canvas2.Visibility = Visibility.Hidden;
            this.Width = 450;
            this.Height = 520;
            button_install.Margin = new Thickness(87, 371, 0, 0);
            button_install.Width = 262;
            button_install.Height = 52;
        }

        public void RunInstallMSI()
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = @"\\network_path\path\program.msi";
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.ErrorDialog = true;
                process.StartInfo.Arguments = "/qb";
                process.StartInfo.Verb = "runasuser";
                process.EnableRaisingEvents = true;
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {

            }
        }

        public void initWebNavInstall()
        {
            check_webnavigator_install();

            if (check_webnavigator_install() == true)
            {
                checkbox_webnavigator.IsEnabled = false;
            }
        }

        public void GetPCName()
        {
            DirectoryEntry entry = new DirectoryEntry("LDAP://bko.borovichi-nov.ru");
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = ("(objectClass=computer)");
            mySearcher.SizeLimit = int.MaxValue;
            mySearcher.PageSize = int.MaxValue;

            foreach (SearchResult resEnt in mySearcher.FindAll())
            {
                //"CN=SGSVG007DC"
                string ComputerName = resEnt.GetDirectoryEntry().Name;
                if (ComputerName.StartsWith("CN="))
                    ComputerName = ComputerName.Remove(0, "CN=".Length);
                pcname.Add(ComputerName);
            }

            mySearcher.Dispose();
            entry.Dispose();
        }

        public List<string> getlnk(string _deskDir)
        {
            lnk.Clear();

            if (Directory.Exists(_deskDir))
            {
                var shortcuts = new DirectoryInfo(_deskDir).GetF‌​iles("*.lnk"); //Получаем ярлыки (.lnk)  с рабочего стола

                foreach (var item in shortcuts)
                {
                    lnk.Add(item.Name);
                }
            }

            else
            {
                MessageBox.Show(_deskDir + " не существует"); 
            }

            return lnk;
        }

        public List<string> getPublicLnk(string _publicDeskDir = @"C:\Users\Public\Desktop")
        {
            Public_lnk.Clear();
            var publicshortcuts = new DirectoryInfo(_publicDeskDir).GetF‌​iles("*.lnk"); //Получаем ярлыки (.lnk)  с общего рабочего стола


            foreach (var item in publicshortcuts)
            {
                Public_lnk.Add(item.Name);
            }

            return Public_lnk;
        }

        public void ConnectionToRemoteComputer()
        {
            string result = "";
            List<string> users = new List<string>();

            

            try
            {
                ConnectionOptions options = new ConnectionOptions();
                //options.Username = "username";
                //options.Password = "password";
                ManagementScope scope = new ManagementScope("\\\\" + pc_combobox.Text + "\\root\\cimv2", options);
                scope.Connect();


                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_UserProfile Where Special = False");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    try
                    {
                        var profileSID = queryObj["SID"].ToString();
                        string full_account = new SecurityIdentifier(profileSID).Translate(typeof(NTAccount)).ToString();

                        if (!full_account.Contains("admin"))
                        {
                            string[] account = full_account.Split(new char[] { '\\' });
                            users.Add(account[1]);

                            userD.Add(profileSID, account[1]);
                            //users_combobox.Items.Add(account[1]);
                        }
                        
                        button_connect.Background = Brushes.LightGreen;
                        button_connect.Content = "Подключено";

                    }
                    catch
                    {

                    }                    
                }

                users.Sort();
                users_combobox.ItemsSource = users;

                
                //query = new ObjectQuery("SELECT Caption FROM Win32_OperatingSystem");
                query = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                searcher = new ManagementObjectSearcher(scope, query);
                ManagementObjectCollection queryCollection = searcher.Get();

                foreach (ManagementObject m in queryCollection)
                {
                    result = m["Caption"].ToString() + "\n" + m["OSArchitecture"].ToString();
                }

                os_info.Content = "ОC на ПК: " + pc_combobox.Text + "\n" + result;

                string softwareRegLoc = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

                ManagementClass registry = new ManagementClass(scope, new ManagementPath("StdRegProv"), null);
                ManagementBaseObject inParams_reg = registry.GetMethodParameters("EnumKey");
                inParams_reg["hDefKey"] = 0x80000002;//HKEY_LOCAL_MACHINE
                inParams_reg["sSubKeyName"] = softwareRegLoc;

                // Read Registry Key Names 
                ManagementBaseObject outParams_reg = registry.InvokeMethod("EnumKey", inParams_reg, null);
                string[] programGuids = outParams_reg["sNames"] as string[];

                foreach (string subKeyName in programGuids)
                {
                    inParams_reg = registry.GetMethodParameters("GetStringValue");
                    inParams_reg["sSubKeyName"] = softwareRegLoc + @"\" + subKeyName;
                    inParams_reg["sValueName"] = "DisplayName";
                    // Read Registry Value 
                    outParams_reg = registry.InvokeMethod("GetStringValue", inParams_reg, null);
                    if (outParams_reg.Properties["sValue"].Value != null)
                    {
                        string softwareName = outParams_reg.Properties["sValue"].Value.ToString();
                        programs.Add(softwareName);
                    }
                }

                if (programs.Any(sublist => sublist.Contains("Microsoft Dynamics AX 2009")))
                {
                    Programms.Content = "Axapta" + " установлена";
                }
                else
                {
                    Programms.Content = "Axapta" + " не установлена";
                }

                if (programs.Any(sublist => sublist.Contains("DIRECTUM 5.7")))
                {
                    Programms.Content += "\n" + "DIRECTUM 5.7" + " установлен";
                }
                else
                {
                    Programms.Content += "\n" + "DIRECTUM 5.7" + " не установлен";
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
           
        }

        public void initNameWebNav()
        {
            List<string> listCheck_cmdKey = check_cmdkey();
            string Tag = "";

            //переберем все контролы на форме
            //Чтобы нашли именно нужные контроллы, не забываем на контроле в свойствах Tag указать нужное наим. 
            foreach (Control control in Canvas1.Children)
            {
                //найдем все checkbox 
                if (control is CheckBox && control.Tag != null)
                {
                    Tag = control.Tag.ToString();

                    if (listCheck_cmdKey.Contains(Tag))
                    {
                        CheckBox cb = control as CheckBox;
                        //cb.Checked = true;
                        cb.IsEnabled = false;
                    }

                }
            }
        }

        public void initLnk()
        {
            string _deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string _publicDeskDir = @"C:\Users\Public\Desktop";
            string Tag = "";

            if (!string.IsNullOrEmpty(pc_combobox.Text))
            {
                _publicDeskDir = @"\\" + pc_combobox.Text + @"\c$\Users\Public\Desktop";
            }

            if(users_combobox.SelectedIndex != -1)
            {
                _deskDir = @"\\" + pc_combobox.Text + @"\c$\Users\" + users_combobox.SelectedValue + @"\Desktop";
            }

            if(users_combobox.SelectedIndex == -1)
            {
                public_lnk.IsChecked = true;
            }
            
            List<string> lnkCheck = getlnk(_deskDir);
            List<string> publicLnkCheck = getPublicLnk(_publicDeskDir);

            if (pc_combobox.SelectedIndex != -1 && users_combobox.SelectedIndex < 0)
                lnkCheck.Clear();

            foreach (Control control in Canvas2.Children)
            {
                if (control is CheckBox && control.Tag != null)
                {
                    Tag = control.Tag.ToString();
                    CheckBox cb = control as CheckBox;

                    if (lnkCheck.Contains(Tag) || publicLnkCheck.Contains(Tag))
                    {
                        cb.IsChecked = false;
                        cb.IsEnabled = false;
                    }
                    else
                    {
                        cb.IsEnabled = true;
                    }
                }
            }
        }

        public static bool check_webnavigator_install()
        {
            string displayName = "";
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

            foreach (String keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName += subkey.GetValue("DisplayName") as string + subkey.GetValue("InstallLocation") as string;
            }

            if (!displayName.Contains("SIMATIC WinCC"))
            {
                //Console.WriteLine("Web navigator not installed");
                //RunInstallMSI();
                return false;
            }
            else
            {
                //Console.WriteLine("Web navigator installed");
                return true;
            }
        }

        public static List<string> check_cmdkey()
        {
            List<string> KeyListValue = new List<string>();
            List<Data> Result = Data.getData();
            var credential_manager = new Credential();

            foreach (var item in Result)
            {
                credential_manager.Target = item.Site.ToString();

                if (credential_manager.Exists())
                {
                    KeyListValue.Add(item.User);
                }

            }

            return KeyListValue;

        }

        public void cmdkey()
        {
            string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            List<string> lnkCheckMonitoring = getlnk(deskDir);

            foreach (Data item in resultCheckbox)
            {
                using (var cred = new Credential())
                {
                    cred.Target = item.Site;
                    cred.Username = item.User;
                    cred.Password = item.Password;
                    cred.Type = CredentialType.Generic;
                    cred.PersistanceType = PersistanceType.LocalComputer;
                    cred.Save();
                }
            }

            if (!lnkCheckMonitoring.Contains("Мониторинг работы производства.lnk") && !Public_lnk.Contains("Мониторинг работы производства.lnk"))
            {
                File.Copy(@"\\network_path\path\_Линки\_Ярлыки новые\Мониторинг работы производства.lnk", Path.Combine(deskDir, "Мониторинг работы производства.lnk"));
            }
        }

        private void urlShortcutToDesktop()
        {
            string deskDir = "";
            string msg = "";

            if (string.IsNullOrEmpty(users_combobox.Text))
            {
                if (public_lnk.IsChecked != true)
                {
                    deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                }
                else
                {
                    deskDir = @"\\" + pc_combobox.Text + @"\c$\Users\Public\Desktop";
                }
            }
            else
            {
                deskDir = @"\\" + pc_combobox.Text + @"\c$\Users\" + users_combobox.SelectedValue + @"\Desktop";
            }

            if (Directory.Exists(deskDir))
            {
                foreach (Data item in lnkCheckbox)
                {
                    if (!File.Exists(deskDir + "\\" + item.link) && !File.Exists(@"\c$\Users\Public\Desktop" + item.link))
                    {
                        File.Copy(@"\\network_path\path\_Линки\_Ярлыки новые\" + item.link, Path.Combine(deskDir, item.link));
                        msg += item.link + "\n";
                    }
                    else
                    {
                        MessageBox.Show("Ярлык " + item.link + " существует");
                    }

                }
            }
            else
            {
                MessageBox.Show("Путь " + deskDir + " не существует");
            }

            MessageBox.Show("Ярлыки перемещены. Путь: " + deskDir + "\n \n" + msg);
            lnkCheckbox.Clear();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (checkbox_webnavigator.IsChecked == true)
            {
                RunInstallMSI();
                checkbox_webnavigator.IsEnabled = false;
            }

            if (resultCheckbox.Count != 0)
                cmdkey();

            if(lnkCheckbox.Count != 0)
                urlShortcutToDesktop();

            initLnk();
            initWebNavInstall();
            initNameWebNav();
        }

        private void subItem1_Click(object sender, RoutedEventArgs e)
        {
            Canvas1.Visibility = Visibility.Visible;
            Canvas2.Visibility = Visibility.Hidden;
            this.Width = 450;
            this.Height = 520;
            button_install.Margin = new Thickness(87,371,0,0);
            button_install.Width = 262;
            button_install.Height = 52;
            CenterWindowOnScreen();
        }

        private void subItem2_Click(object sender, RoutedEventArgs e)
        {
            string result = "";
            Canvas1.Visibility = Visibility.Hidden;
            Canvas2.Visibility = Visibility.Visible;
            this.Width = 1471;
            this.Height = 840;
            button_install.Margin = new Thickness(87, 707, 0, 0);
            button_install.Width = 800;
            button_install.Height = 52;
            CenterWindowOnScreen();

            if(button_connect.Background != Brushes.LightGreen)
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem");
                ManagementObjectCollection queryCollection = searcher.Get();

                foreach (ManagementObject m in queryCollection)
                {
                    result = m["Caption"].ToString() + "\n" + m["OSArchitecture"].ToString();
                    //result += "\n" + m["OSArchitecture"].ToString();
                }

                os_info.Content = "OC на ПК:" + "\n" + result;
            }

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            List<Data> checkTag = Data.getData();
            string Tag = "";
            CheckBox cbCheck;

            foreach (Control control in Canvas1.Children)
            {
                //найдем все checkbox 
                if (control is CheckBox && control.Tag != null)
                {
                    cbCheck = control as CheckBox;

                    if (cbCheck.IsChecked != true)
                        continue;

                    Tag = control.Tag.ToString();

                    foreach (var item in checkTag)
                    {
                        if(item.User == Tag)
                            resultCheckbox.Add(new Data { Site = item.Site, User = item.User, Password = item.Password, });
                    }
                }
            }

        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            List<Data> checkTag = Data.getData();
            string Tag = "";
            CheckBox cbCheck;

            foreach (Control control in Canvas1.Children)
            {
                //найдем все checkbox 
                if (control is CheckBox && control.Tag != null)
                {
                    cbCheck = control as CheckBox;

                    if (cbCheck.IsChecked != true)
                        continue;

                    Tag = control.Tag.ToString();

                    foreach (var item in checkTag)
                    {
                        if (item.User == Tag)
                            resultCheckbox.Remove(new Data { Site = item.Site, User = item.User, Password = item.Password, });
                    }
                }
            }
        }

        private void CheckBox_Checked_Link(object sender, RoutedEventArgs e)
        {
            List<Data> checkTag = Data.getData();
            string Tag = "";
            CheckBox cbCheck;

            foreach (Control control in Canvas2.Children)
            {
                //найдем все checkbox 
                if (control is CheckBox && control.Tag != null)
                {
                    Tag = control.Tag.ToString();
                    cbCheck = control as CheckBox;

                    if (cbCheck.IsChecked != true)
                        continue;

                    if (!lnkCheckbox.Exists(x => x.link == Tag))
                        lnkCheckbox.Add(new Data { link = Tag });
                }
            }
        }

        private void CheckBox_Unchecked_Link(object sender, RoutedEventArgs e)
        {
            List<Data> checkTag = Data.getData();
            List<string> UncheckedLink = new List<string>();
            string Tag = "";
            CheckBox cbCheck;

            foreach (Control control in Canvas2.Children)
            {
                //найдем все checkbox 
                if (control is CheckBox && control.Tag != null)
                {
                    Tag = control.Tag.ToString();
                    cbCheck = control as CheckBox;

                    if (cbCheck.IsChecked == false)
                    {
                        UncheckedLink.Add(Tag); //запишем все теги чекбоксов на Canvas2, у которых снята галочка
                    }
                }
            }

            lnkCheckbox.RemoveAll(t => UncheckedLink.Contains(t.link));
        }

        public void RefreshComboboxItems()
        {
            string textToSearch = pc_combobox.Text.ToUpper();

            pc_combobox.Items.Clear();

            foreach (var pc in pcname)
            {
                if (pc.Contains(textToSearch))
                {
                    pc_combobox.Items.Add(pc);
                }
            }
        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        public void ping()
        {
            if (pc_combobox.SelectedValue == null)
                return;

            Ping PingRemotePC = new Ping();
            PingReply reply = PingRemotePC.Send(pc_combobox.SelectedValue.ToString(), 1000);
            string status = reply.Status.ToString();
            button_connect.IsEnabled = false;

            if (status == "Success")
            {
                button_connect.IsEnabled = true;
            }
            else
            {
                button_connect.IsEnabled = false;
                MessageBox.Show("Удаленный компьютер выключен");
            }
        }

        private void Button_connect_Click(object sender, RoutedEventArgs e)
        {
            ConnectionToRemoteComputer();
            initLnk();
            public_lnk.IsChecked = true;
        }

        private void Pc_combobox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshComboboxItems();
            programs.Clear();
            Programms.Content = "";

            if (pc_combobox.SelectedIndex != -1)
                initLnk();

            if (string.IsNullOrEmpty(pc_combobox.Text))
            {
                users_combobox.SelectedIndex = -1;
                button_connect.IsEnabled = false;
                button_connect.Background = Brushes.WhiteSmoke;
                button_connect.Content = "Подключиться";
                users_combobox.Text = "";
                users_combobox.ItemsSource = null;
                //users_combobox.Items.Clear();
                lnkCheckbox.Clear();
            }
        }

        private void Users_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            public_lnk.IsChecked = false;
            initLnk();
        }

        private void Pc_combobox_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void all_check_uncheck_click(object sender, RoutedEventArgs e)
        {
            foreach (Control control in Canvas2.Children)
            {
                if (control is CheckBox && control.Tag != null)
                {
                    CheckBox cb = control as CheckBox;

                    if ((String)all_check_uncheck.Content == "Выбрать все" && cb.IsEnabled == true)
                    {
                        cb.IsChecked = true;
                    }
                    else
                    {
                        cb.IsChecked = false;                        
                    }
                }
            }

            if((String)all_check_uncheck.Content == "Выбрать все")
            {
                all_check_uncheck.Content = "Cнять все";
            }
            else
            {
                all_check_uncheck.Content = "Выбрать все";
            }
        }

        private void Pc_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ping();
        }

        private void Public_lnk_Checked(object sender, RoutedEventArgs e)
        {
            users_combobox.SelectedIndex = -1;
        }
    }
}
