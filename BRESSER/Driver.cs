//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Telescope driver for BRESSER Goto
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM Telescope interface version: <To be completed by driver developer>
// Author:		HeShousheng <graycode@qq.com>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// 13-02-2018	HeShousheng	1.0.0	Initial edit, created from ASCOM driver template
//              HeShousheng 1.0.1 and 1.0.2, Skip 
// 28-06-2018	HeShousheng	1.0.3	Add souce code in install file
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code canbe deleted and this definition removed.
#define Telescope

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;

//user
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Data;

namespace ASCOM.BRESSER
{
    //
    // Your driver's DeviceID is ASCOM.BRESSER.Telescope
    //
    // The Guid attribute sets the CLSID for ASCOM.BRESSER.Telescope
    // The ClassInterface/None addribute prevents an empty interface called
    // _BRESSER from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM Telescope Driver for BRESSER.
    /// </summary>
    [Guid("4d61bd1e-c30a-4a39-a248-b4d9cca649c0")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Telescope : ITelescopeV3
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.BRESSER.Telescope";
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        //private static string driverDescription = "ASCOM Telescope Driver for BRESSER.";
        private static string driverDescription = "BRESSER Goto ASCOM Driver";

        internal static string comPortProfileName = "COM Port"; // Constants used for Profile persistence
        internal static string comPortDefault = "COM1";
        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "false";

        internal static string comPort; // Variables to hold the currrent device configuration

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool connectedState;

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private Util utilities;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
        /// </summary>
        private AstroUtils astroUtilities;
        //public  Astrometry.NOVAS.INOVAS2 astrotool;

        /// <summary>
        /// Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        internal static TraceLogger tl;

        //AlignmentMode
        public AlignmentModes TAlignmentMode;
        //SerialPort
        public SerialPort MySerialPort;
        public int BaudRate;
        byte[] RecvTemp = new byte[2];
        public byte[] SendBuf = new byte[13];
        public byte[] RecvBuf = new byte[9];
        public byte headernum=0,datanum=0,recvnum=0;
        public bool RecvFlag=false;

        //Telescope Status
        public bool m_bAtHome;
        public bool m_bAtPark;
        public bool m_bSlewing;
        public bool m_bTracking;

        //Target parameter
        public double m_dAltitude, m_dAzimuth;
        public double m_dDeclination, m_dRightAscension;
        public double m_dTargetDeclination, m_dTargetRightAscension;
        public double m_dSyncDeclination, m_dSyncRightAscension;

        //Guider parameter
        public double m_dGuideRateDeclination, m_dGuideRateRightAscension;
        public bool m_bIsPulseGuiding;

        //Site infomation
        public double m_dSiteLatitude;
        public double m_dSiteLongitude;
        public double m_dSiteElevation;

        //Optical parameter
        public double m_dApertureDiameter;
        public double m_dApertureArea;
        public double m_dFocalLength;


        //UTC 
        public DateTime m_DUTCDate;

        //Coordinate transformation
        public const double Rad=57.29577951;
        struct CAA2DCoordinate
        {
            public double X;
            public double Y;
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="BRESSER"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Telescope()
        {
            tl = new TraceLogger("", "BRESSER");
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl.LogMessage("Telescope", "Starting initialisation");

            connectedState = false; // Initialise connected to false
            utilities = new Util(); //Initialise util object
            astroUtilities = new AstroUtils(); // Initialise astro utilities object
            //TODO: Implement your additional construction here

            MySerialPort = new SerialPort();
            //Protocol header
            SendBuf[0] = 0x55;
            SendBuf[1] = 0xAA;
            SendBuf[2] = 0x01;
            SendBuf[3] = 0x09;

            TAlignmentMode = AlignmentModes.algPolar;

            m_bAtHome = false;
            m_bAtPark = false;
            m_bSlewing = false;
            m_bTracking = false;

            //m_dAltitude = 90;
            //m_dAzimuth = 0;
            //m_dRightAscension = 0;
            //m_dDeclination = 0;

            //Synchronism
            m_dSyncDeclination = 0;
            m_dSyncRightAscension=0;

            m_bIsPulseGuiding = false;

            m_DUTCDate = DateTime.Parse("2000-01-01 00:00:00");

            tl.LogMessage("Telescope", "Completed initialisation");
        }


        //
        // PUBLIC COM INTERFACE ITelescopeV3 IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (IsConnected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // Call CommandString and return as soon as it finishes
            this.CommandString(command, raw);
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
            // DO NOT have both these sections!  One or the other
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            string ret = CommandString(command, raw);
            // TODO decode the return string and return true or false
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBool");
            // DO NOT have both these sections!  One or the other
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time

            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            // Clean up the tracelogger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            astroUtilities.Dispose();
            astroUtilities = null;
        }

        public bool Connected
        {
            get
            {
                LogMessage("Connected", "Get {0}", IsConnected);
                return IsConnected;
            }
            set
            {
                tl.LogMessage("Connected", "Set {0}", value);
                if (value == IsConnected)
                    return;

                if (value)
                {
                    connectedState = true;
                    LogMessage("Connected Set", "Connecting to port {0}", comPort);
                    // TODO connect to the device
                    ConnectDevice();
                }
                else
                {
                    connectedState = false;
                    LogMessage("Connected Set", "Disconnecting from port {0}", comPort);
                    // TODO disconnect from the device
                    DisconnectDevice();
                }
            }
        }

        public string Description
        {
            // TODO customise this device description
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
                string driverInfo = "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                LogMessage("InterfaceVersion Get", "3");
                return Convert.ToInt16("3");
            }
        }

        public string Name
        {
            get
            {
                string name = "Short driver name - please customise";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ITelescope Implementation
        public void AbortSlew()
        {
            //tl.LogMessage("AbortSlew", "Not implemented");
            //throw new ASCOM.MethodNotImplementedException("AbortSlew");
            SendCmd(0x1D, 0.0, 0.0);
            Thread.Sleep(1000);//Delay for next command
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                //tl.LogMessage("AlignmentMode Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("AlignmentMode", false);
                //SendCmd(0x21, 0.0, 0.0);
                //Thread.Sleep(1000);
                //return TAlignmentMode;
                return AlignmentModes.algPolar;//All process as Polar mode
            }
        }

        public double Altitude
        {
            get
            {
                //tl.LogMessage("Altitude", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("Altitude", false);
                return m_dAltitude;
            }
        }

        public double ApertureArea
        {
            get
            {
                //tl.LogMessage("ApertureArea Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("ApertureArea", false);
                return m_dApertureArea;
            }
        }

        public double ApertureDiameter
        {
            get
            {
                //tl.LogMessage("ApertureDiameter Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("ApertureDiameter", false);
                return m_dApertureDiameter;
            }
        }

        public bool AtHome
        {
            get
            {
                tl.LogMessage("AtHome", "Get - " + false.ToString());
                return false;
            }
        }

        public bool AtPark
        {
            get
            {
                //tl.LogMessage("AtPark", "Get - " + false.ToString());
                //return false;
                return m_bAtPark;
            }
        }

        public IAxisRates AxisRates(TelescopeAxes Axis)
        {
            tl.LogMessage("AxisRates", "Get - " + Axis.ToString());
            return new AxisRates(Axis);
        }

        public double Azimuth
        {
            get
            {
                //tl.LogMessage("Azimuth Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("Azimuth", false);
                return m_dAzimuth;
            }
        }

        public bool CanFindHome
        {
            get
            {
                tl.LogMessage("CanFindHome", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanMoveAxis(TelescopeAxes Axis)
        {
            tl.LogMessage("CanMoveAxis", "Get - " + Axis.ToString());
            switch (Axis)
            {
                case TelescopeAxes.axisPrimary: return false;
                case TelescopeAxes.axisSecondary: return false;
                case TelescopeAxes.axisTertiary: return false;
                default: throw new InvalidValueException("CanMoveAxis", Axis.ToString(), "0 to 2");
            }
        }

        public bool CanPark
        {
            get
            {
                //tl.LogMessage("CanPark", "Get - " + false.ToString());
                //return false;
                return true;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                //tl.LogMessage("CanPulseGuide", "Get - " + false.ToString());
                //return false;
                return true;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                tl.LogMessage("CanSetDeclinationRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                //tl.LogMessage("CanSetGuideRates", "Get - " + false.ToString());
                //return false;
                return true;
            }
        }

        public bool CanSetPark
        {
            get
            {
                tl.LogMessage("CanSetPark", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                tl.LogMessage("CanSetPierSide", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                tl.LogMessage("CanSetRightAscensionRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                tl.LogMessage("CanSetTracking", "Get - " + false.ToString());
                return false;
                //return true;
            }
        }

        public bool CanSlew  //Set ture to enable SlewToCoordinates in SkyChart 
        {
            get
            {
                //tl.LogMessage("CanSlew", "Get - " + false.ToString());
                //return false;
                return true;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                tl.LogMessage("CanSlewAltAz", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                tl.LogMessage("CanSlewAltAzAsync", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                //tl.LogMessage("CanSlewAsync", "Get - " + false.ToString());
                //return false;
                return true;
            }
        }

        public bool CanSync
        {
            get
            {
                //tl.LogMessage("CanSync", "Get - " + false.ToString());
                //return false;
                return true;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                tl.LogMessage("CanSyncAltAz", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanUnpark
        {
            get
            {
                //tl.LogMessage("CanUnpark", "Get - " + false.ToString());
                //return false;
                return true;
            }
        }

        public double Declination
        {
            get
            {
                //double declination = 0.0;
                //tl.LogMessage("Declination", "Get - " + utilities.DegreesToDMS(declination, ":", ":"));
                //return declination;
                return m_dDeclination;
            }
        }

        public double DeclinationRate
        {
            get
            {
                double declination = 0.0;
                tl.LogMessage("DeclinationRate", "Get - " + declination.ToString());
                return declination;
            }
            set
            {
                tl.LogMessage("DeclinationRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("DeclinationRate", true);
            }
        }

        public PierSide DestinationSideOfPier(double RightAscension, double Declination)
        {
            tl.LogMessage("DestinationSideOfPier Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DestinationSideOfPier", false);
        }

        public bool DoesRefraction
        {
            get
            {
                tl.LogMessage("DoesRefraction Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("DoesRefraction", false);
            }
            set
            {
                tl.LogMessage("DoesRefraction Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("DoesRefraction", true);
            }
        }

        public EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equLocalTopocentric;
                tl.LogMessage("DeclinationRate", "Get - " + equatorialSystem.ToString());
                return equatorialSystem;
            }
        }

        public void FindHome()
        {
            tl.LogMessage("FindHome", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("FindHome");
        }

        public double FocalLength
        {
            get
            {
                //tl.LogMessage("FocalLength Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("FocalLength", false);
                return m_dFocalLength;
            }
        }

        public double GuideRateDeclination
        {
            get
            {
                //tl.LogMessage("GuideRateDeclination Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
                return m_dGuideRateDeclination;
            }
            set
            {
                //tl.LogMessage("GuideRateDeclination Set", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
                m_dGuideRateDeclination = value;
                SendCmd(0x0A, m_dGuideRateRightAscension, m_dGuideRateDeclination);
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                //tl.LogMessage("GuideRateRightAscension Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", false);
                return m_dGuideRateRightAscension;
            }
            set
            {
                //tl.LogMessage("GuideRateRightAscension Set", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", true);
                m_dGuideRateRightAscension = value;
                SendCmd(0x0A, m_dGuideRateRightAscension, m_dGuideRateDeclination);
            }
        }

        public bool IsPulseGuiding
        {
            get
            {
                //tl.LogMessage("IsPulseGuiding Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("IsPulseGuiding", false);
                return m_bIsPulseGuiding;
            }
        }

        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            tl.LogMessage("MoveAxis", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("MoveAxis");
        }

        public void Park()
        {
            //tl.LogMessage("Park", "Not implemented");
            //throw new ASCOM.MethodNotImplementedException("Park");
            SendCmd(0x1E, 0.0, 0.0);
            m_bAtPark = true;
        }

        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            //tl.LogMessage("PulseGuide", "Not implemented");
            //throw new ASCOM.MethodNotImplementedException("PulseGuide");
            byte GuideDir = 0;
            switch (Direction)
            {
                case GuideDirections.guideEast:
                    GuideDir = 1;
                    break;
                case GuideDirections.guideWest:
                    GuideDir = 2;
                    break;
                case GuideDirections.guideNorth:
                    GuideDir = 4;
                    break;
                case GuideDirections.guideSouth:
                    GuideDir = 8;
                    break;
            }
            SendPulse(GuideDir,Duration);
        }

        public double RightAscension
        {
            get
            {
                //double rightAscension = 0.0;
                //tl.LogMessage("RightAscension", "Get - " + utilities.HoursToHMS(rightAscension));
                //return rightAscension;
                return m_dRightAscension;
            }
        }

        public double RightAscensionRate
        {
            get
            {
                double rightAscensionRate = 0.0;
                tl.LogMessage("RightAscensionRate", "Get - " + rightAscensionRate.ToString());
                return rightAscensionRate;
            }
            set
            {
                tl.LogMessage("RightAscensionRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("RightAscensionRate", true);
            }
        }

        public void SetPark()
        {
            tl.LogMessage("SetPark", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SetPark");
        }

        public PierSide SideOfPier
        {
            get
            {
                tl.LogMessage("SideOfPier Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SideOfPier", false);
            }
            set
            {
                tl.LogMessage("SideOfPier Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SideOfPier", true);
            }
        }

        public double SiderealTime
        {
            get
            {
                // get greenwich sidereal time: https://en.wikipedia.org/wiki/Sidereal_time
                //double siderealTime = (18.697374558 + 24.065709824419081 * (utilities.DateUTCToJulian(DateTime.UtcNow) - 2451545.0));

                // alternative using NOVAS 3.1
                double siderealTime = 0.0;
                using (var novas = new ASCOM.Astrometry.NOVAS.NOVAS31())
                {
                    var jd = utilities.DateUTCToJulian(DateTime.UtcNow);
                    novas.SiderealTime(jd, 0, novas.DeltaT(jd),
                        ASCOM.Astrometry.GstType.GreenwichApparentSiderealTime,
                        ASCOM.Astrometry.Method.EquinoxBased,
                        ASCOM.Astrometry.Accuracy.Reduced, ref siderealTime);
                }
                // allow for the longitude
                siderealTime += SiteLongitude / 360.0 * 24.0;
                // reduce to the range 0 to 24 hours
                siderealTime = siderealTime % 24.0;
                tl.LogMessage("SiderealTime", "Get - " + siderealTime.ToString());
                return siderealTime;
            }
        }

        public double SiteElevation
        {
            get
            {
                //tl.LogMessage("SiteElevation Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("SiteElevation", false);
                return m_dSiteElevation;
            }
            set
            {
                //tl.LogMessage("SiteElevation Set", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("SiteElevation", true);
                m_dSiteElevation = value;
                WriteProfile();
            }
        }

        public double SiteLatitude
        {
            get
            {
                //tl.LogMessage("SiteLatitude Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("SiteLatitude", false);
                SendCmd(0x1F, 0.0, 0.0);
                Thread.Sleep(1000);
                return m_dSiteLatitude;
            }
            set
            {
                //tl.LogMessage("SiteLatitude Set", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("SiteLatitude", true);
                m_dSiteLatitude = value;
                //MessageBox.Show("m_dSiteLatitude~"+m_dSiteLatitude.ToString ());
                SendCmd(0x25, m_dSiteLongitude, m_dSiteLatitude);
                WriteProfile();
            }
        }

        public double SiteLongitude
        {
            get
            {
                //tl.LogMessage("SiteLongitude Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("SiteLongitude", false);
                return m_dSiteLongitude;
            }
            set
            {
                //tl.LogMessage("SiteLongitude Set", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("SiteLongitude", true);
                m_dSiteLongitude = value;
                SendCmd(0x25, m_dSiteLongitude, m_dSiteLatitude);
                WriteProfile();
            }
        }

        public short SlewSettleTime
        {
            get
            {
                tl.LogMessage("SlewSettleTime Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", false);
            }
            set
            {
                tl.LogMessage("SlewSettleTime Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", true);
            }
        }

        public void SlewToAltAz(double Azimuth, double Altitude)
        {
            tl.LogMessage("SlewToAltAz", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SlewToAltAz");
        }

        public void SlewToAltAzAsync(double Azimuth, double Altitude)
        {
            tl.LogMessage("SlewToAltAzAsync", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SlewToAltAzAsync");
            //MessageBox.Show("SlewToAltAzAsync");
        }

        public void SlewToCoordinates(double RightAscension, double Declination)
        {
            //tl.LogMessage("SlewToCoordinates", "Not implemented");
            //throw new ASCOM.MethodNotImplementedException("SlewToCoordinates");
            m_dTargetRightAscension = RightAscension;
            m_dTargetDeclination = Declination;
            //Subtract Synchronism
            SendCmd(0x23, RightAscension-m_dSyncRightAscension, Declination-m_dSyncDeclination);
            //MessageBox.Show("SlewToCoordinates");
        }

        public void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            //tl.LogMessage("SlewToCoordinatesAsync", "Not implemented");
            //throw new ASCOM.MethodNotImplementedException("SlewToCoordinatesAsync");
            SlewToCoordinates(RightAscension, Declination);
            //MessageBox.Show("SlewToCoordinatesAsync");
        }

        public void SlewToTarget()
        {
            //tl.LogMessage("SlewToTarget", "Not implemented");
            //throw new ASCOM.MethodNotImplementedException("SlewToTarget");
            SlewToCoordinates(m_dTargetRightAscension, m_dTargetDeclination);
            //MessageBox.Show("SlewToTarget");
        }

        public void SlewToTargetAsync()
        {
            //tl.LogMessage("SlewToTargetAsync", "Not implemented");
            //throw new ASCOM.MethodNotImplementedException("SlewToTargetAsync");
            SlewToCoordinatesAsync(m_dTargetRightAscension, m_dTargetDeclination);
            //MessageBox.Show("SlewToTargetAsync");
        }

        public bool Slewing
        {
            get
            {
                //tl.LogMessage("Slewing Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("Slewing", false);
                return m_bSlewing;
            }
        }

        public void SyncToAltAz(double Azimuth, double Altitude)
        {
            tl.LogMessage("SyncToAltAz", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SyncToAltAz");
        }

        public void SyncToCoordinates(double RightAscension, double Declination)
        {
            //tl.LogMessage("SyncToCoordinates", "Not implemented");
            //throw new ASCOM.MethodNotImplementedException("SyncToCoordinates");
            //计算同步量，需要加入上次累积量
            m_dSyncRightAscension = RightAscension - m_dTargetRightAscension + m_dSyncRightAscension;
            m_dSyncDeclination = Declination - m_dTargetDeclination + m_dSyncDeclination;
            //MessageBox.Show("SyncToCoordinates");
        }

        public void SyncToTarget()
        {
            //tl.LogMessage("SyncToTarget", "Not implemented");
            //throw new ASCOM.MethodNotImplementedException("SyncToTarget");
            SyncToCoordinates(m_dTargetRightAscension, m_dTargetDeclination);
            //MessageBox.Show("SyncToTarget");
        }

        public double TargetDeclination
        {
            get
            {
                //tl.LogMessage("TargetDeclination Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("TargetDeclination", false);
                return m_dTargetDeclination;
            }
            set
            {
                //tl.LogMessage("TargetDeclination Set", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("TargetDeclination", true);
                m_dTargetDeclination = value;
            }
        }

        public double TargetRightAscension
        {
            get
            {
                //tl.LogMessage("TargetRightAscension Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("TargetRightAscension", false);
                return m_dTargetRightAscension;
            }
            set
            {
                //tl.LogMessage("TargetRightAscension Set", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("TargetRightAscension", true);
                m_dTargetRightAscension = value;
            }
        }

        public bool Tracking
        {
            get
            {
                //bool tracking = true;
                //tl.LogMessage("Tracking", "Get - " + tracking.ToString());
                //return tracking;
                return m_bTracking;
            }
            set
            {
                //tl.LogMessage("Tracking Set", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("Tracking", true);
                m_bTracking = value;
            }
        }

        public DriveRates TrackingRate
        {
            get
            {
                tl.LogMessage("TrackingRate Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("TrackingRate", false);
            }
            set
            {
                tl.LogMessage("TrackingRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("TrackingRate", true);
            }
        }

        public ITrackingRates TrackingRates
        {
            get
            {
                ITrackingRates trackingRates = new TrackingRates();
                tl.LogMessage("TrackingRates", "Get - ");
                foreach (DriveRates driveRate in trackingRates)
                {
                    tl.LogMessage("TrackingRates", "Get - " + driveRate.ToString());
                }
                return trackingRates;
            }
        }

        public DateTime UTCDate
        {
            get
            {
                DateTime utcDate = DateTime.UtcNow;
                //tl.LogMessage("TrackingRates", "Get - " + String.Format("MM/dd/yy HH:mm:ss", utcDate));
                return utcDate;
                //return m_DUTCDate;
                //throw new ASCOM.PropertyNotImplementedException("UTCDate", false);
            }
            set
            {
                //tl.LogMessage("UTCDate Set", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("UTCDate", true);
                SendTime(0x26);
            }
        }

        public void Unpark()
        {
            //tl.LogMessage("Unpark", "Not implemented");
            //throw new ASCOM.MethodNotImplementedException("Unpark");
            //SendCmd(0x1E, 0.0, 0.0);
            m_bAtPark = false;
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered. 
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "Telescope";
                if (bRegister)
                {
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    P.Unregister(driverID);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected
        {
            get
            {
                // TODO check that the driver hardware connection exists and is connected to the hardware
                return connectedState;
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Telescope";
                tl.Enabled = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
                comPort = driverProfile.GetValue(driverID, comPortProfileName, string.Empty, comPortDefault);
                BaudRate = Convert.ToInt32(driverProfile.GetValue(driverID, "BaudRate", string.Empty, "9600"));

                //TAlignmentMode = Convert.ToInt32(driverProfile.GetValue(driverID, "AlignmentMode", string.Empty, "1")) == 0 ? AlignmentModes.algAltAz : AlignmentModes.algPolar;

                m_dSiteLongitude = Convert.ToDouble(driverProfile.GetValue(driverID, "SiteLongitude", string.Empty, "102.7"));
                m_dSiteLatitude = Convert.ToDouble(driverProfile.GetValue(driverID, "SiteLatitude", string.Empty, "25.0"));
                m_dSiteElevation = Convert.ToDouble(driverProfile.GetValue(driverID, "SiteElevation", string.Empty, "1800"));

                m_dApertureDiameter = Convert.ToDouble(driverProfile.GetValue(driverID, "ApertureDiameter", string.Empty, "200"));
                m_dApertureArea = Convert.ToDouble(driverProfile.GetValue(driverID, "ApertureArea", string.Empty, "31400"));
                m_dFocalLength = Convert.ToDouble(driverProfile.GetValue(driverID, "FocalLength", string.Empty, "2000"));
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Telescope";
                driverProfile.WriteValue(driverID, traceStateProfileName, tl.Enabled.ToString());
                driverProfile.WriteValue(driverID, comPortProfileName, comPort.ToString());
                driverProfile.WriteValue(driverID, "BaudRate", BaudRate.ToString());

                //driverProfile.WriteValue(driverID, "AlignmentMode", (TAlignmentMode == AlignmentModes.algAltAz ? 0 : 1).ToString());

                driverProfile.WriteValue(driverID, "SiteLongitude", m_dSiteLongitude.ToString());
                driverProfile.WriteValue(driverID, "SiteLatitude", m_dSiteLatitude.ToString());
                driverProfile.WriteValue(driverID, "SiteElevation", m_dSiteElevation.ToString());

                driverProfile.WriteValue(driverID, "ApertureDiameter", m_dApertureDiameter.ToString());
                driverProfile.WriteValue(driverID, "ApertureArea", m_dApertureArea.ToString());
                driverProfile.WriteValue(driverID, "FocalLength", m_dFocalLength.ToString());
            }
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        internal static void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            tl.LogMessage(identifier, msg);
        }
        #endregion

        #region User properties and methods

        public void ConnectDevice()
        {
            UartInitial();
        }

        public void DisconnectDevice()
        {
            UartUninitial();
        }

        public void UartInitial()
        {
            try
            {
                if (MySerialPort.IsOpen)
                    MySerialPort.Close();
                MySerialPort.PortName = comPort;
                MySerialPort.DataBits = 8;
                MySerialPort.Parity = Parity.None;
                MySerialPort.StopBits = StopBits.One;
                MySerialPort.BaudRate = BaudRate;
                MySerialPort.NewLine = "\r\n";
                MySerialPort.DataReceived += new SerialDataReceivedEventHandler(UartRecvMsg);
                MySerialPort.Open();
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void UartUninitial()
        {
            try
            {
                if (MySerialPort.IsOpen)
                {
                    MySerialPort.BreakState = true;
                    MySerialPort.Close();
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void UartSendMsg(byte[] Msg,int Cnt)
        {
            try
            {
                if (!MySerialPort.IsOpen)
                    return;
                MySerialPort.Write(Msg,0,Cnt);
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void UartRecvMsg(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            int num=MySerialPort.BytesToRead;//RecvFlag
            if (num > 0)
            {
                byte RecvByte = 0;
                for (int i = 0; i < num; i++)
                {
                    if(RecvFlag==false)
                    {
                        MySerialPort.Read(RecvTemp, 0, 1);
                        RecvByte = RecvTemp[0];
                        //str+=RecvByte.ToString("X2");
                        //MessageBox.Show(RecvByte.ToString ("X2"));
	                    switch(headernum)
		                { 
		                   case 0:
		                        if(RecvByte==0x55)
			                        headernum++; 
			                    break;
		                   case 1:
		                        if(RecvByte==0xAA)
			                        headernum++;
			                    else
			                        headernum=0;
			                    break;
			                case 2:	
			                    if(RecvByte==0x01)
			                        headernum++;
			                    else
			                        headernum=0;
			                    break;
			                case 3:
			                    if(RecvByte<10)
				                {
			                        datanum=RecvByte; 
				                    headernum++;
				                }
				                else
				                    headernum=0;
			                    break;
			                case 4:
                                if(recvnum<datanum) 
					            {
                                    RecvBuf[recvnum]=RecvByte;
				                    recvnum++;
                                    if(recvnum>=datanum) 
                                    {
                                        RecvFlag=true;
				                        headernum=0;
				                        recvnum=0;
                                    }
					            }
			                    break;
		                }
                        //MessageBox.Show("headernum:" + headernum.ToString() + "  recvnum:" + recvnum.ToString() + "   RecvFlag:" + RecvFlag.ToString());
   	                }
                }
            }
            if (RecvFlag == true)
            {
                //MessageBox.Show(str);
                ProcessCommand(RecvBuf);
            }
        }
        //
        public void ProcessCommand(byte[] Command)
        {
            byte[] TmpDat1 = new byte[4];
            byte[] TmpDat2 = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                TmpDat1[i] = Command[i + 1];
                TmpDat2[i] = Command[i + 5];
            }
            if (Command[0] == 0xFE) //Site information
            {
                m_dSiteLongitude = Convert.ToDouble(BitConverter.ToSingle(TmpDat1, 0)); ;
                m_dSiteLatitude = Convert.ToDouble(BitConverter.ToSingle(TmpDat2, 0));
                //MessageBox.Show(m_dSiteLongitude.ToString());
            }
            else if (Command[0] == 0xF0) //Ra & Dec ---UnTracked
            {
                m_dRightAscension = Convert.ToDouble(BitConverter.ToSingle(TmpDat1, 0))+m_dSyncRightAscension;
                m_dDeclination = Convert.ToDouble(BitConverter.ToSingle(TmpDat2, 0))+m_dSyncDeclination;

                //CAA2DCoordinate m_tmpAzAlt = Equatorial2Horizontal(m_dRightAscension, m_dDeclination, m_dSiteLatitude);
                //MessageBox.Show(m_dRightAscension.ToString() + "***" + m_dDeclination.ToString ());
                //m_dAzimuth = m_tmpAzAlt.X;
                //m_dAltitude = m_tmpAzAlt.Y;

                m_bTracking = false;
            }
            else if (Command[0] == 0xFF) //Ra & Dec ---Tracked
            {
                m_dRightAscension = Convert.ToDouble(BitConverter.ToSingle(TmpDat1, 0)) + m_dSyncRightAscension;
                m_dDeclination = Convert.ToDouble(BitConverter.ToSingle(TmpDat2, 0)) + m_dSyncDeclination;

                //CAA2DCoordinate m_tmpAzAlt = Equatorial2Horizontal(m_dRightAscension, m_dDeclination, m_dSiteLatitude);
                //m_dAzimuth = m_tmpAzAlt.X;
                //m_dAltitude = m_tmpAzAlt.Y;
                //MessageBox.Show(m_dRightAscension.ToString() + "***" + m_dDeclination.ToString());

                m_bTracking = true;
            }
            else if (Command[0] == 0x50) 
                TAlignmentMode = AlignmentModes.algPolar;
            else if (Command[0] == 0x41)
                TAlignmentMode = AlignmentModes.algAltAz;
            //Reset flag
            RecvFlag = false;
        }
        //////////////
        //Command byte
        //SendBuf[4]=0x01,0x02,0x04,0x08---Guide Dir
        //SendBuf[4]=0x0A---Guide rate
        //SendBuf[4]=0x1D---Abort Slew
        //SendBuf[4]=0x1E---Park
        //SendBuf[4]=0x1F---Get Site info
        //SendBuf[4]=0x20---Get DateTime
        //SendBuf[4]=0x21---Get AlignmentMode
        //SendBuf[4]=0x22---Disconnect
        //SendBuf[4]=0x23---Goto
        //SendBuf[4]=0x24---Sync
        //SendBuf[4]=0x25---Set Site info
        //SendBuf[4]=0x26---Set DateTime
        //////////////
        void SendCmd(byte cmd, double dat1, double dat2)
        {
            byte[] bDAT1 = new byte[4];
            byte[] bDAT2 = new byte[4];
            bDAT1 = BitConverter.GetBytes(Convert.ToSingle(dat1));
            bDAT2 = BitConverter.GetBytes(Convert.ToSingle(dat2));
            SendBuf[4] = cmd;
            SendBuf[5] = bDAT1[0]; SendBuf[6] = bDAT1[1]; SendBuf[7] = bDAT1[2]; SendBuf[8] = bDAT1[3];
            SendBuf[9] = bDAT2[0]; SendBuf[10] = bDAT2[1]; SendBuf[11] = bDAT2[2]; SendBuf[12] = bDAT2[3];
            UartSendMsg(SendBuf, 13);
        }
        //Set DateTime
        void SendTime(byte cmd)
        {
            SendBuf[4] = cmd;
            SendBuf[5] = (byte)(DateTime.Now.Year / 100);
            SendBuf[6] = (byte)(DateTime.Now.Year % 100);
            SendBuf[7] = (byte)DateTime.Now.Month;
            SendBuf[8] = (byte)DateTime.Now.Day;
            SendBuf[9] = (byte)DateTime.Now.Hour;
            SendBuf[10] = (byte)DateTime.Now.Minute;
            SendBuf[11] = (byte)DateTime.Now.Second;
            TimeZone localZone = TimeZone.CurrentTimeZone;
            DateTime currentDate = DateTime.Now;
            TimeSpan currentOffset = localZone.GetUtcOffset(currentDate);
            SendBuf[12] = (byte)(currentOffset.Hours + 12);
            UartSendMsg(SendBuf, 13);
        }
        //Guider
        void SendPulse(byte cmd,int Duration)
        {
            byte[] bDAT1 = new byte[4];
            byte[] bDAT2 = new byte[4];
            bDAT1 = BitConverter.GetBytes(Duration);
            bDAT2 = BitConverter.GetBytes(Duration);
            SendBuf[4] = cmd;
            SendBuf[5] = bDAT1[0]; SendBuf[6] = bDAT1[1]; SendBuf[7] = bDAT1[2]; SendBuf[8] = bDAT1[3];
            SendBuf[9] = bDAT2[0]; SendBuf[10] = bDAT2[1]; SendBuf[11] = bDAT2[2]; SendBuf[12] = bDAT2[3];
            UartSendMsg(SendBuf, 13);
        }
        
        // Coordinate conversion
        static double DegreesToRadians(double Degrees)
        {
            return Degrees * 0.017453292519943295769236907684886;
        }
        
        static double RadiansToDegrees(double Radians)
        {
            return Radians * 57.295779513082320876798154814105;
        }

        static double RadiansToHours(double Radians)
        {
            return Radians * 3.8197186342054880584532103209403;
        }

        static double HoursToRadians(double Hours)
        {
            return Hours * 0.26179938779914943653855361527329;
        }

        static double HoursToDegrees(double Hours)
        {
            return Hours * 15;
        }

        static double DegreesToHours(double Degrees)
        {
            return Degrees / 15;
        }
       
        CAA2DCoordinate Equatorial2Horizontal(double Ra, double Dec, double Lat)
        {
            double LocalHourAngle = SiderealTime - Ra;
            while (LocalHourAngle >= 12.0) LocalHourAngle -= 24.0;
            while (LocalHourAngle < -12.0) LocalHourAngle += 24.0;
            //////////////
            LocalHourAngle = HoursToRadians(LocalHourAngle);
            //Delta = DegreesToRadians(Delta);
            double Delta = DegreesToRadians(Dec);
            Lat = DegreesToRadians(Lat);

            CAA2DCoordinate Horizontal;
            Horizontal.X = RadiansToDegrees(Math.Atan2(Math.Sin(LocalHourAngle), Math.Cos(LocalHourAngle)*Math.Sin(Lat) - Math.Tan(Delta)*Math.Cos(Lat)));
            //if (Horizontal.X < 0)
            //    Horizontal.X += 360;
            Horizontal.X += 180;
            Horizontal.Y = RadiansToDegrees(Math.Asin(Math.Sin(Lat)*Math.Sin(Delta) + Math.Cos(Lat)*Math.Cos(Delta)*Math.Cos(LocalHourAngle)));
            return Horizontal;
        }
        
        CAA2DCoordinate Horizontal2Equatorial(double Azi, double Alt, double Lat)
        {
            //Convert from degress to radians
            Azi = DegreesToRadians(Azi);
            Alt = DegreesToRadians(Alt);
            Lat = DegreesToRadians(Lat);

            CAA2DCoordinate Equatorial;
            Equatorial.X = RadiansToHours(Math.Atan2(Math.Sin(Azi), Math.Cos(Azi)*Math.Sin(Lat) + Math.Tan(Alt)*Math.Cos(Lat)));

            Equatorial.X = (SiderealTime - Equatorial.X);
            if (Equatorial.X < 0)
                Equatorial.X += 24;
            Equatorial.Y = RadiansToDegrees(Math.Asin(Math.Sin(Lat)*Math.Sin(Alt) - Math.Cos(Lat)*Math.Cos(Alt)*Math.Cos(Azi)));
            //MessageBox.Show(Equatorial.Y.ToString ());
            return Equatorial;
        }
        
        //Save data
        public void WriteValue(string key, string value)
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Telescope";
                driverProfile.WriteValue(driverID, key, value);
            }
        }

        public void WriteValue(string key, int value)
        {
            WriteValue(key, value.ToString());
        }

        public void WriteValue(string key, double value)
        {
            WriteValue(key, value.ToString());
        }

        public void WriteValue(string key, bool value)
        {
            //this.WriteValue(key, ((value == true) ? 1 : 0).ToString());
            WriteValue(key, ((value == true) ? 1 : 0).ToString());
        }
        //Read data
        public string ReadValue(string key, string defaultvalue)
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Telescope";
                return driverProfile.GetValue(driverID, key, string.Empty, defaultvalue);
            }
        }

        public int ReadValue(string key, int defaultvalue)
        {
            return Int32.Parse(ReadValue(key, defaultvalue.ToString()));
        }

        public double ReadValue(string key, double defaultvalue)
        {
            return Double.Parse(ReadValue(key, defaultvalue.ToString()));
        }

        public bool ReadValue(string key, bool defaultvalue)
        {
            return ReadValue(key, Convert.ToInt32(defaultvalue)) == 0 ? false : true;
        }

        #endregion
    }
}
