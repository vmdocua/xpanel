using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace XPanel
{
	public partial class AboutForm : Form
	{
		public AboutForm()
		{
			InitializeComponent();
			labelVer.Text = "Version: "+Assembly.GetExecutingAssembly().GetName().Version.ToString();
		}
	}
}