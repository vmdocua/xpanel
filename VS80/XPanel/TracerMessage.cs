using System;
using System.Collections.Generic;
using System.Text;
//
using Docsultant.Flex.LocalConnection;

namespace XPanel
{
	class TracerMessage 
	{
		public const int LEVEL_INFO  = 2;
		public const int LEVEL_WARN  = 4;
		public const int LEVEL_ERROR = 8;
		//
		private LCMessage m_msg;

		public TracerMessage()
		{
		}

		public void Attach(LCMessage msg)
		{
			m_msg = msg;
		}

		public int Level
		{
			get 
			{
				return m_msg.tokens[3].IntValue;
			}
		}

		public String LevelText
		{
			get 
			{
				int l = Level;
				switch(l)
				{
					case LEVEL_INFO  : return "INFO";
					case LEVEL_WARN  : return "WARN";
					case LEVEL_ERROR : return "ERROR";
					default:
						return "UNKOWN:"+l;
				}
			}
		}


		public string Message
		{
			get
			{
				return m_msg.tokens[4].StringValue;
			}
		}


	}
}
