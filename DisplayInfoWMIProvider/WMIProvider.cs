//DisplayInfoWMIProvider (c) 2009 by Roger Zander

using System;
using System.Collections;
using System.Management.Instrumentation;
using System.DirectoryServices;
using System.Management;
using Microsoft.Win32;
using System.Text;

using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;


[assembly: WmiConfiguration(@"root\cimv2", HostingModel = ManagementHostingModel.LocalSystem)]
namespace DisplayInfoWMIProvider
{
    [System.ComponentModel.RunInstaller(true)]
    public class MyInstall : DefaultManagementInstaller
    {
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
            System.Runtime.InteropServices.RegistrationServices RS = new System.Runtime.InteropServices.RegistrationServices();
            try
            {
                new System.EnterpriseServices.Internal.Publish().GacInstall(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch { }
        }

        public override void Uninstall(IDictionary savedState)
        {

            try
            {
                ManagementClass MC = new ManagementClass(@"root\cimv2:Win32_MonitorDetails");
                MC.Delete();
            }
            catch { }

            try
            {
                base.Uninstall(savedState);
            }
            catch { }

            try
            {
                new System.EnterpriseServices.Internal.Publish().GacRemove(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch { }
        }
    }

    [ManagementEntity(Name = "Win32_MonitorDetails")]
    public class DisplayDetails
    {
        [ManagementKey]
        public string PnPID { get; set; }

        /// <summary>
        /// SerialNumber of the Monitor
        /// </summary>
        [ManagementProbe]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Monitor Model
        /// </summary>
        [ManagementProbe]
        public string Model { get; set; }

        [ManagementProbe]
        public string MonitorID { get; set; }

        /// <summary>
        /// Monitor Name (as in Device manager)
        /// </summary>
        [ManagementProbe]
        public string Name { get; set; }

        /// <summary>
        /// Diagonal Size in Inch
        /// </summary>
        [ManagementProbe]
        public string SizeDiagInch { get; set; }

        /// <summary>
        /// Horizontal Size in Centimeter
        /// </summary>
        [ManagementProbe]
        public string SizeHorCM { get; set; }

        /// <summary>
        /// Vertical Size in Centimeter
        /// </summary>
        [ManagementProbe]
        public string SizeVerCM { get; set; }


        /// <summary>
        /// The Constructor to create a new instances of the DisplayDetails class...
        /// </summary>
        public DisplayDetails(string sPnPID, string sSerialNumber, string sModel, string sMonitorID, string sName, string sSize, string sHSize, string sVSize )
        {
            PnPID = sPnPID;
            SerialNumber = sSerialNumber;
            Model = sModel;
            MonitorID = sMonitorID;
            Name = sName;
            SizeDiagInch = sSize;
            SizeHorCM = sHSize;
            SizeVerCM = sVSize;
        }

        /// <summary>
        /// This Function returns all Monitor Details
        /// </summary>
        /// <returns></returns>
        [ManagementEnumerator]
        static public IEnumerable GetMonitorDetails()
        {
            List<string> sKeys = new List<string>();
            System.Management.ManagementObjectSearcher MOS = new System.Management.ManagementObjectSearcher("root\\cimv2", "SELECT * FROM Win32_PnPEntity WHERE Service = 'monitor'");
            foreach (ManagementObject MO in MOS.Get())
            {
                string sSerial = "";
                string sModel = "";
                string sName = "";
                string sHWID = "";
                string sSize = "";
                string sHSize = "";
                string sVSize = "";
                string sPnPDeviceID = "";
                try
                {
                    sPnPDeviceID = MO["PNPDeviceID"].ToString();
                    sKeys.Add(@"SYSTEM\CurrentControlSet\Enum\" + sPnPDeviceID + @"\Device Parameters");
                    string sKey = @"SYSTEM\CurrentControlSet\Enum\" + sPnPDeviceID + @"\Device Parameters";
                    RegistryKey Display = Registry.LocalMachine.OpenSubKey(sKey, false);

                    sName = MO["Name"].ToString();
                    sHWID = ((string[])MO["HardwareID"])[0].ToString().Replace("MONITOR\\", "");

                    //Define Search Keys
                    string sSerFind = new string(new char[] { (char)00, (char)00, (char)00, (char)0xff });
                    string sModFind = new string(new char[] { (char)00, (char)00, (char)00, (char)0xfc });

                    //Get the EDID code
                    byte[] bObj = Display.GetValue("EDID", null) as byte[];
                    if (bObj != null)
                    {
                        //Get the 4 Vesa descriptor blocks
                        string[] sDescriptor = new string[4];
                        sDescriptor[0] = Encoding.Default.GetString(bObj, 0x36, 18);
                        sDescriptor[1] = Encoding.Default.GetString(bObj, 0x48, 18);
                        sDescriptor[2] = Encoding.Default.GetString(bObj, 0x5A, 18);
                        sDescriptor[3] = Encoding.Default.GetString(bObj, 0x6C, 18);


                        try
                        {
                            double iHorizontal = double.Parse(((byte)bObj.GetValue(0x15)).ToString());
                            double iVertical = double.Parse(((byte)bObj.GetValue(0x16)).ToString());
                            sHSize = iHorizontal.ToString();
                            sVSize = iVertical.ToString();

                            double dDiag = Math.Sqrt((iHorizontal * iHorizontal) + (iVertical * iVertical)) * 0.3937007874015748;
                            sSize = Math.Round(dDiag, 1).ToString();
                        }
                        catch { }

                        //Search the Keys
                        foreach (string sDesc in sDescriptor)
                        {
                            if (sDesc.Contains(sSerFind))
                            {
                                sSerial = sDesc.Substring(4).Replace("\0", "").Trim();
                            }
                            if (sDesc.Contains(sModFind))
                            {
                                sModel = sDesc.Substring(4).Replace("\0", "").Trim();
                            }
                        }


                    }
                }
                catch { continue; }

                if (!string.IsNullOrEmpty(sPnPDeviceID + sSerial + sModel + sHWID + sName))
                {
                    yield return new DisplayDetails(sPnPDeviceID, sSerial, sModel, sHWID, sName, sSize, sHSize, sVSize);
                }


            }

            
        }
    }
}
