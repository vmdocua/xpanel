using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace Docsultant.Flex.LocalConnection
{
	public unsafe class LocalConnection
	{
		private string             m_name;
		private IntPtr             m_mutex;
		private IntPtr             m_mutexExit;
		private IntPtr[]           m_mutexes;
		private IntPtr             m_file;
		private bool               m_connected;
		private byte*              m_buff;
		private AMFRawDeserializer m_rd;
		private LCMessage          m_lastMsg;
		private volatile LCSink    m_sink;
		private Thread             m_t;
		private volatile bool      m_tAlive;
		private volatile bool      m_started;

		const int LISTENERS_OFFSET = 40976;
		const int BLOCK_SIZE       = 65535;

		// interop helpers
		[
			DllImport("kernel32.dll"/*, PreserveSig=false*/)
		]
		private static extern IntPtr CreateMutex(
			IntPtr lpMutexAttributes,
			bool bInitialOwner,
			string lpName
		);

		[
			DllImport("kernel32.dll"/*, PreserveSig=false*/)
		]
		private static extern IntPtr OpenMutex(UInt32 dwAccess, bool bInheritHandle, string lpName);
		private const UInt32 MUTEX_ALL_ACCESS = 0x001f0001;

		[
			DllImport("kernel32.dll")
		]
		private static extern bool ReleaseMutex(IntPtr hMutex);

		[
			DllImport("kernel32.dll")
		]
		private static extern bool CloseHandle(IntPtr hMutex);

		[
			DllImport("kernel32.dll")
		]
		private static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);


		[
			DllImport("kernel32.dll")
		]
		private static extern UInt32 WaitForMultipleObjects(
			int nCount,
			IntPtr[] lpHandles,
			bool bWaitAll,
			UInt32 dwMilliseconds
		);

		//
		private const UInt32 WAIT_OBJECT_0  = 0;
		private const UInt32 WAIT_OBJECT_1  = WAIT_OBJECT_0+1;
		private const UInt32 WAIT_TIMEOUT   = 0x00000102;
		private const UInt32 WAIT_ABANDONED = 0x00000080;
		//
		[
			DllImport("kernel32.dll")
		]
		private static extern IntPtr OpenFileMapping(
				UInt32 dwDesiredAccess,
				bool bInheritHandle,
				string lpName);
		//
		private const UInt32 FILE_MAP_ALL_ACCESS = 0x000f001f;
		//
		[
			DllImport("kernel32.dll")
		]
		private static extern IntPtr MapViewOfFile(
				IntPtr hFileMappingObject,
				UInt32 dwDesiredAccess,
				UInt32 dwFileOffsetHigh,
				UInt32 dwFileOffsetLow,
				UInt32 dwNumberOfBytesToMap);

		[
			DllImport("kernel32.dll")
		]
		private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

		public LocalConnection(string name)
		{
			m_name      = name;
			m_mutex     = IntPtr.Zero;
			m_mutexExit = IntPtr.Zero;
			m_file      = IntPtr.Zero;
			m_connected = false;
			m_rd        = new AMFRawDeserializer();
			m_lastMsg   = new LCMessage();
			m_sink      = null;
			m_mutexes   = new IntPtr[] { IntPtr.Zero, IntPtr.Zero };
			m_tAlive    = false;
			m_started   = false;
		}

		public void Advise(LCSink sink)
		{
			m_sink = sink;
		}

		private void Connect()
		{
			int i = LISTENERS_OFFSET;
			bool alreadyConnected = false;

			while( (i<BLOCK_SIZE) && m_buff[i]!=0 ) 
			{
				IntPtr p = new IntPtr(&m_buff[i]);
				string s = Marshal.PtrToStringAnsi(p);
				if( !alreadyConnected && m_name==s )
				{
					alreadyConnected = true;
					Log("Already connected: "+s);
				} else
				{
					Log(""+s);
				}
				i += s.Length+1;
			}

			if( i>BLOCK_SIZE )
			{
				Log("Failed connecting: "+m_name);
				return;
			}

			m_connected = true;

			if( i>0 && !alreadyConnected )
			{
				Log("Registering");
				IntPtr hName = Marshal.StringToHGlobalAnsi(m_name);
				if( hName!=IntPtr.Zero )
				{
					byte* p2 = (byte*)hName;
					while( *p2!=0 )
					{
						m_buff[i] = *p2;
						i++;
						p2++;
					}
					m_buff[i] = 0;
					Marshal.FreeHGlobal(hName);
				}
			}
		}

		private void HandleMessage()
		{
			//Log("HandleMessage(), msg="+m_lastMsg);
			// TODO: lock on exit
			if( m_sink!=null && m_started )
				m_sink.OnLcMessage(m_lastMsg);
		}

		private void Log(string s)
		{
			Console.WriteLine(s);
		}

		public bool Poll()
		{
			if( !m_started )
				return false;

			if( m_mutex==IntPtr.Zero )
			{
				m_mutex = OpenMutex(MUTEX_ALL_ACCESS, false, "MacromediaMutexOmega");
				m_mutexes[0] = m_mutex;
				if( m_mutex==IntPtr.Zero )
				{
					//Log("Waiting for mutex...");
					UInt32 res2 = WaitForSingleObject(m_mutexExit, 1500);
					if( res2==WAIT_OBJECT_0 )
						return false; // break poll
					return true;
				}
			}

			int sleepTime = 10;
			bool readMsg = false;

			//Log("Start waiting for mutex2...");
			UInt32 res = WaitForMultipleObjects(2, m_mutexes, false, 100000);
			//Log("End waiting for mutex2: "+res);
			//int le = Marshal.GetLastWin32Error();
			//UInt32 res = WaitForSingleObject(m_mutex, 1000);
			switch( res )
			{
				case WAIT_OBJECT_0:
					{
						try
						{
							if( m_file==IntPtr.Zero )
							{
								m_file = OpenFileMapping(FILE_MAP_ALL_ACCESS, false,
									"MacromediaFMOmega");
								if( m_file==IntPtr.Zero )
								{
									//Log("Failed openning MMF");
									// TODO: sleep after releasing mutex
									sleepTime = 500;
								}
							}

							if( m_file!=IntPtr.Zero )
							{
								IntPtr lpMem = MapViewOfFile(m_file, FILE_MAP_ALL_ACCESS,
									0, 0, 0);
								try
								{
									if( lpMem!=IntPtr.Zero )
									{
										m_buff = (byte*)lpMem;
										if( !m_connected )
											Connect();

										if( m_buff[16]!=0 )
										{
											//string dst = Marshal.PtrToStringAnsi(new IntPtr(&m_buff[19]), m_buff[18]);
											//if( m_name==dst )
											{
												//Log("dst: "+dst);
												if( !ReadMessage() )
													sleepTime = 20;
												else
												{
													sleepTime = 0;
													readMsg = true;
												}
											}
										}
									}
								} finally 
								{
									m_buff = null;
									if( lpMem!=IntPtr.Zero )
										if( !UnmapViewOfFile(lpMem) )
											Log("UnmapViewOfFile() failed");
								}
							}
						} finally
						{
							ReleaseMutex(m_mutex);
						}
					}
					break;

				case WAIT_OBJECT_1:
					return false; // break poll

				default:
						sleepTime = 200;
					break;
			}


			if( readMsg && m_started )
				HandleMessage();

			// TODO: optimize sleep
			if( sleepTime>0 )
				Sleep(sleepTime);
			return true;
		}

		private bool ReadMessage()
		{
			uint size      = *(uint*)&m_buff[12];
			uint timestamp = *(uint*)&m_buff[8];
			// check timestamp and size
			if( size==0 )
				return false;

			m_rd.Attach(&m_buff[16], (int)size);
			AMFToken t = m_rd.ReadToken();
			if( t.kind!=AMFToken.K_STRING ) return false;
			if( m_name!=t.StringValue ) return false;

			m_rd.Attach(&m_buff[16], (int)size);
			AMFToken[] tokens = m_rd.ReadAll();

			m_lastMsg.Attach((int)timestamp, (int)size, tokens);

			// mark message as "read"
			m_buff[8]  = 0;
			m_buff[12] = 0;

			//HandleMessage();
			return true;
		}

		private void Sleep(int time)
		{
			System.Threading.Thread.Sleep(time);
		}

		public void Start()
		{
			if( m_mutexExit==IntPtr.Zero )
			{
				m_mutexExit  = CreateMutex(IntPtr.Zero, false, null);
				m_mutexes[1] = m_mutexExit;
				WaitForSingleObject(m_mutexExit, 0);
			}

			m_tAlive = false;
			m_started = true;

			m_t = new Thread(new ThreadStart(this.ThreadProc));
			m_t.Name = "LC Thread: "+m_name;
			m_t.ApartmentState = ApartmentState.STA;
			m_t.IsBackground = true;
			m_t.Start();
		}

		public void Stop()
		{
			// TODO: make more intelligent 
			m_started = false;
			ReleaseMutex(m_mutexExit);

			for(int i=0; i<5; i++)
			{
				Sleep(100);
				if( !m_tAlive )
					break;
			}

			if( m_tAlive )
				m_t.Abort();

			if( m_mutexExit!=IntPtr.Zero )
			{
				CloseHandle(m_mutexExit);
				m_mutexExit = IntPtr.Zero;
			}

			if( m_mutex!=IntPtr.Zero )
			{
				CloseHandle(m_mutex);
				m_mutex = IntPtr.Zero;
			}

			if( m_file!=IntPtr.Zero )
			{
				CloseHandle(m_file);
				m_file = IntPtr.Zero;
			}
		}

		private void ThreadProc()
		{
			m_tAlive = true;
			try
			{
				if( m_sink!=null )
					try{ m_sink.OnLcStart(m_name); } catch(Exception) {}

				//Log("Enter LC cycle");

				while(true)
				{
					try
					{
						bool res = true;
						try {
							//Log("Enter poll");
							res = Poll();
						} finally {
							//Log("Leave poll: "+res);
						}

						if( !res )
							break;
					} catch(Exception e) {
						if( m_sink!=null && m_started )
							 try { m_sink.OnLcError("Exception", e); } catch(Exception) {}
					}
				}

				//Log("Leave LC cycle");

				if( m_sink!=null && m_started )
					try{ m_sink.OnLcStop(); } catch(Exception) {}
			} finally {
				m_tAlive = false;
			}
		}

	}
}
