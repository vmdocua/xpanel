namespace Docsultant.Flex.LocalConnection
{
	public class LCMessage
	{
		public int        time;
		public int        size;
		public AMFToken[] tokens;

		public void Attach(int _time, int _size, AMFToken[] _t)
		{
			time = _time;
			size = _size;
			tokens = _t;
		}

		public override string ToString()
		{
			string s = "LCMessage";
			s += "\n time : "+time;
			s += "\n size : "+size;
			for(int i=0; i<tokens.Length; i++)
				s += "\n token["+i+"] "+tokens[i].val;
			return s;
		}
	}
}


