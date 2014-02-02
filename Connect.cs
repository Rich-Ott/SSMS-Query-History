using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.SqlServer.Management.Smo.RegSvrEnum;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.VisualStudio.CommandBars;

namespace SSMSQueryHistory
{
    /// <summary>The object for implementing an Add-in.</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2, IDTCommandTarget
    {
        /// <summary>
        /// Guid string of the SSMSQueryHistory add-in
        /// </summary>
        private const string SSMSQueryHistoryGuid = "{b30aae7c-e969-4d35-8ed1-27c2c3a6228c}";

        private CommandEvents ce;
        private DTE2 _applicationObject;
        private AddIn _addInInstance;

        /// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
        public Connect()
        {
        }


        /// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification
        /// that the Add-in is being loaded.</summary>
        /// <param term='application'>Root object of the host application.</param>
        /// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
        /// <param term='addInInst'>Object representing this Add-in.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _applicationObject = (DTE2)application;
            _addInInstance = (AddIn)addInInst;

            if (connectMode == ext_ConnectMode.ext_cm_UISetup)
            {
                Commands2 commands = (Commands2)_applicationObject.Commands;
                string toolsMenuName = "Tools";
                object[] contextGUIDs = new object[] { };

                CommandBar menuBarCommandBar = ((CommandBars)_applicationObject.CommandBars)["MenuBar"];
                CommandBarControl toolsControl = menuBarCommandBar.Controls[toolsMenuName];
                CommandBarPopup toolsPopup = (CommandBarPopup)toolsControl;

                try
                {
                    Command command = commands.AddNamedCommand2(
                        _addInInstance,
                        "SSMSQueryHistory",
                        "SSMS Query History",
                        "Opens the SSMS Query History search window",
                        true,
                        183,
                        ref contextGUIDs,
                        (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled,
                        (int)vsCommandStyle.vsCommandStylePictAndText,
                        vsCommandControlType.vsCommandControlTypeButton);

                    if ((command != null) && (toolsPopup != null))
                    {
                        command.AddControl(toolsPopup.CommandBar, 1);
                    }
                }
                catch (System.ArgumentException)
                {
                }
            }
        }

        /// <summary>
        /// Writes TSQL query history to an XML file
        /// </summary>
        /// <param name="Guid"></param>
        /// <param name="ID"></param>
        /// <param name="customIn"></param>
        /// <param name="customOut"></param>
        public void CommandEvents_AfterExecute(string Guid, int ID, object customIn, object customOut)
        {
            EnvDTE.TextDocument doc = (EnvDTE.TextDocument)ServiceCache.ExtensibilityModel.ActiveDocument.Object(null);
            if (doc != null)
            {
                string Sql = GetSQLString(doc);

                if (String.IsNullOrWhiteSpace(Sql))
                    return;

                // Get Current Connection Information
                UIConnectionInfo connInfo = ServiceCache.ScriptFactory.CurrentlyActiveWndConnectionInfo.UIConnectionInfo;

                // Set the path to the history file
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SSMS Query History",
                    DateTime.Now.ToString("yyyy-MMM-dd"));

                // Create the history file path if it doesn't exist
                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);

                // Set the filename of the history file
                string filename = Path.Combine(path, "QueryHistory.xml");

                // Attempt to load the history file and create an empty file if it doesn't exist
                XDocument history;
                try
                {
                    history = XDocument.Load(filename);
                }
                catch (FileNotFoundException)
                {
                    history = XDocument.Parse("<SSMSQueryHistory/>");
                }

                // Add the newly executed TSQL query
                history.Root.Add(
                    new XElement("HistoryEntry",
                        new XElement("Server", new XCData(connInfo.ServerName ?? String.Empty)),
                        new XElement("Database", new XCData(connInfo.AdvancedOptions["DATABASE"])),
                        new XElement("DateTime", DateTime.Now),
                        new XElement("Query", new XCData(Sql))));

                // Save the history file
                history.Save(filename);
            }
        }

        private string GetSQLString(EnvDTE.TextDocument doc)
        {
            /*
            String selectedText = doc.Selection.Text;

            if (selectedText.Length == 0)
            {
                // get all text in window
                doc.Selection.SelectAll();
                selectedText = doc.Selection.Text;
            }
            return selectedText;
             */

            // Update from Matt P.
            String selectedText = doc.Selection.Text;

            if (selectedText.Length == 0)
            {
                // Nothing is selected, so get all text in the editor window
                var editPoint = doc.StartPoint.CreateEditPoint();
                selectedText = editPoint.GetText(doc.EndPoint);
            }
            return selectedText;
        }

        /// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
        /// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
        }

        /// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        /// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification
        /// that the host application has completed loading.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnStartupComplete(ref Array custom)
        {
            // Get a handle to the Execute SQL command
            ce = _addInInstance.DTE.Events.get_CommandEvents("{52692960-56BC-4989-B5D3-94C47A513E8D}", 1);

            // Subscribe to the AfterExecute event handler to log queries
            ce.AfterExecute += new _dispCommandEvents_AfterExecuteEventHandler(CommandEvents_AfterExecute);
        }

        /// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification
        /// that the host application is being unloaded.</summary>
        /// <param term='custom'>Array of parameters that are host application specific.</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnBeginShutdown(ref Array custom)
        {
        }

        /// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the
        /// command's availability is updated</summary>
        /// <param term='commandName'>The name of the command to determine state for.</param>
        /// <param term='neededText'>Text that is needed for the command.</param>
        /// <param term='status'>The state of the command in the user interface.</param>
        /// <param term='commandText'>Text requested by the neededText parameter.</param>
        /// <seealso class='Exec' />
        public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
        {
            if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
            {
                if (commandName == "SSMSQueryHistory.Connect.SSMSQueryHistory")
                {
                    status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                    return;
                }
            }
        }

        private Window toolWin = null;

        /// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
        /// <param term='commandName'>The name of the command to execute.</param>
        /// <param term='executeOption'>Describes how the command should be run.</param>
        /// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
        /// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
        /// <param term='handled'>Informs the caller if the command was handled or not.</param>
        /// <seealso class='Exec' />
        public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
        {
            handled = false;
            if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
            {
                if (commandName == "SSMSQueryHistory.Connect.SSMSQueryHistory")
                {
                    handled = true;

                    if (toolWin == null)
                    {
                        Windows2 win = (Windows2)_addInInstance.DTE.Windows;
                        object ctlObject = null;
                        toolWin = win.CreateToolWindow2(_addInInstance, Assembly.GetExecutingAssembly().Location, "SSMSQueryHistory.SSMSQueryHistoryControl", "Query History Search", SSMSQueryHistoryGuid, ref ctlObject);
                    }

                    if (toolWin != null)
                    {
                        toolWin.Visible = true;
                    }
                    return;
                }
            }
        }
    }
}
