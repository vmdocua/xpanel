using System;
using System.Collections.Generic;
using System.Text;

namespace Docsultant.Flex.LocalConnection
{
	public interface LCSink
	{
		void OnLcStart(string name);
		void OnLcStop();
		void OnLcError(string desc, object code);
		void OnLcMessage(LCMessage msg);
	}
}
