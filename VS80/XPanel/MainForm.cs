using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//
using Docsultant.Flex.LocalConnection;

namespace XPanel
{
	public partial class MainForm : Form
	{
		private LocalConnection     m_lc;
		private RichTextBoxLCSink   m_sink;

		public MainForm()
		{
			InitializeComponent();
			InitLC();
		}

		private void InitLC()
		{
			m_lc = new LocalConnection("_tracer");
			m_sink = new RichTextBoxLCSink(textBoxLog);
			m_lc.Advise(m_sink);
			m_lc.Start();
			showTimeToolStripMenuItem.Checked = m_sink.ShowTime;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// TODO: Stop lc
			Close();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			m_lc.Stop();
		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			textBoxLog.Text = "";
		}

		private void showTimeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			m_sink.ShowTime = !m_sink.ShowTime;
			showTimeToolStripMenuItem.Checked = m_sink.ShowTime;
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AboutForm dlg = new AboutForm();
			dlg.ShowDialog(this);
		}
	}
}