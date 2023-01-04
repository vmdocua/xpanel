using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;

namespace Docsultant.Flex.LocalConnection
{
	public unsafe class AMFRawDeserializer
	{
		private byte* m_buff;
		private int   m_offset;
		private int   m_size;

		public void Attach(byte* buff, int size)
		{
			m_buff = buff;
			m_offset = 0;
			m_size = size;
		}

		public void Detach()
		{
			m_buff   = null;
			m_offset = 0;
			m_size   = 0;
		}

		public AMFToken[] ReadAll()
		{
			ArrayList lst = new ArrayList(8);
			AMFToken t = null;
			while(!_EOF())
			{
				t = ReadToken();
				lst.Add(t);
			}

			AMFToken[] res = new AMFToken[lst.Count];
			lst.CopyTo(res);
			return res;
		}

		public AMFToken ReadToken()
		{
			byte k = _ReadByte();
			switch(k)
			{
				case AMFToken.K_STRING:
						int len  = _ReadWord();
						string s = _ReadString(len);
					return new AMFToken(k, s);

				case AMFToken.K_NUMBER:
					Int64 n = _ReadInt64();
					return new AMFToken(k, n);

				default:
					throw new NotImplementedException("Unsupported AMF token: "+k);
			}
		}

		private bool _EOF()
		{
			return m_offset>=m_size;
		}

		private byte _BigByte2Little(byte b)
		{
			byte b2 = 0;
			for(int i=0; i<8; i++)
			{
				b2 = (byte)((b2 << 1) | (byte)(b & 0x01));
				b  = (byte)(b >> 1);
			}
			return b2;
		}

		private byte _ReadByte()
		{
			return m_buff[m_offset++];
		}

		private string _ReadString(int len)
		{
			if( len==0 ) return "";
			string s = Marshal.PtrToStringAnsi(new IntPtr((void*)&m_buff[m_offset]), len);
			m_offset += len;
			return s;
		}

		private ushort _ReadWord()
		{
			ushort u = (ushort)(((ushort)(m_buff[m_offset] << 8)) | (ushort)m_buff[m_offset+1]);
			m_offset += 2;
			return u;
		}

		private long _ReadInt64()
		{
			long l = 0;

			for(int i=(m_offset+7); i>=m_offset; i--)
			{
				l = (l << 8 ) | _BigByte2Little(m_buff[i]);
			}
			m_offset += 8;
			return l;
		}

	}
}
