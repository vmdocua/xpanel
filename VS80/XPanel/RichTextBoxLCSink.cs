using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
//
using Docsultant.Flex.LocalConnection;

namespace XPanel
{
	class RichTextBoxLCSink : LCSink
	{
		private readonly bool  m_debug;
		private RichTextBox    m_textBox;
		private bool           m_showTime;
		private TracerMessage  m_msg;

		public RichTextBoxLCSink(RichTextBox _textBox)
		{
			m_debug = false;
			//m_debug = true;
			m_textBox = _textBox;
			m_showTime = true;
			m_msg = new TracerMessage();
		}

		public bool ShowTime
		{
			get { return m_showTime;  }
			set { m_showTime = value; }
		}

		private delegate void LogTextDelegate(string s);
		private void LogText(string s)
		{
			if( m_textBox.InvokeRequired )
			{
				if( m_showTime )
					s = DateTime.Now.ToLongTimeString()+" "+s;
				m_textBox.Invoke(new LogTextDelegate(this.LogText), new object[] {s});
				return;
			}
			m_textBox.AppendText("\n"+s);
		}

		#region LCSink Members

		public void OnLcStart(string name)
		{
			if( m_debug )
				LogText("Start listening: "+name);
		}

		public void OnLcStop()
		{
			if( m_debug )
				LogText("Stop listening");
		}

		public void OnLcError(string desc, object code)
		{
			LogText("Internal error: "+desc+", "+code);
		}

		public void OnLcMessage(LCMessage msg)
		{
			if( m_debug )
				LogText("LCMessage: "+msg);

			m_msg.Attach(msg);
			LogText("["+m_msg.LevelText+"] "+m_msg.Message);
		}

		#endregion
	}
}
