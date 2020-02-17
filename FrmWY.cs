using EdmApiClientLibrary;
using EdmApiClientLibrary.Enums;
using EdmApiClientLibrary.Models;
using EmailWY.CommonHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Deployment.Application;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmailWY
{
    public partial class FrmWY : Form
    {

        public FrmWY()
        {
            InitializeComponent();

            BrEmail.ScriptErrorsSuppressed = true;
            //BrEmail.Url = new Uri(loginUrl);
            BrEmail.Navigate(loginUrl);

        }







    }
}
