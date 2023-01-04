using System;
using System.Collections.Generic;
using System.Text;

namespace Docsultant.Flex.LocalConnection
{
	public class AMFToken
	{
		public const byte K_NUMBER           = 0x00;
		public const byte K_BOOLEAN          = 0X01;
		public const byte K_STRING           = 0x02;
		public const byte K_OBJECT           = 0x03;
		public const byte K_UNDEFINED        = 0x06;
		public const byte K_REFERENCE        = 0x07;
		public const byte K_ASSOCIATIVEARRAY = 0x08;
		public const byte K_ARRAY            = 0x0A;
		public const byte K_DATE             = 0x0B;
		public const byte K_SIMPLEOBJECT     = 0x0D;
		public const byte K_XML              = 0x0F;
		public const byte K_CLASS            = 0x10;

		public readonly byte   kind;
		public readonly Object val;

		public AMFToken(byte _k, Object _val)
		{
			kind = _k;
			val  = _val;
		}

		public int IntValue
		{
			get
			{
				return (int)((Int64)val);
			}
		}

		public long LongValue
		{
			get
			{
				return (long)((Int64)val);
			}
		}

		public String StringValue
		{
			get { return (String)val; }
		}
	}
}
