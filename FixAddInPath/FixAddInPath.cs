using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;

namespace FixAddInPath
{
    [RunInstaller(true)]
    public partial class FixAddInPath : System.Configuration.Install.Installer
    {
        public FixAddInPath()
        {
            InitializeComponent();
        }

        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);

            string targetDirectory = Context.Parameters["targetdir"];
            string addinDirectory = Context.Parameters["addindir"];
            string exePath = String.Format("{0}SSMSQueryHistory.dll", targetDirectory);
            string addinFile = String.Format("{0}SSMSQueryHistory.AddIn", addinDirectory);

            try
            {
                XDocument doc = XDocument.Load(addinFile);

                var elements = from a in doc.Descendants(XName.Get("Assembly",
                                   doc.Root.GetDefaultNamespace().NamespaceName))
                               select a;

                elements.First().Value = exePath;
                doc.Save(addinFile);
            }
            catch (Exception)
            {

            }
        }
    }
}
