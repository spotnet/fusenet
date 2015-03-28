using System;
using System.IO;
using System.Collections;
using System.Security.Cryptography;

//-------------------------------------------------------------
//
//    Fusenet - The Future of Usenet
//              http://github.com/fusenet
//
//    This library is free software; you can redistribute it
//    and modify it under the terms of the GNU General Public
//    License as published by the Free Software Foundation.
//
//-------------------------------------------------------------

namespace Phuse
{
    internal class CRC32 : HashAlgorithm // Phil Bolduc
	{
		private uint m_crc;
        protected uint[] crc32Table;

        protected static bool autoCache;
        protected static uint AllOnes = 0xffffffff;
        protected static Hashtable cachedCRC32Tables;

        public CRC32() : this(DefaultPolynomial) { }
        public CRC32(uint aPolynomial) : this(aPolynomial, CRC32.AutoCache) { }

        public uint[] CurrentTable { get { return crc32Table; } }
		public static uint DefaultPolynomial { get { return 0x04C11DB7; } }

		public static bool AutoCache
		{
			get { return autoCache; }
			set { autoCache = value; }
		}

		static CRC32()
		{
			cachedCRC32Tables = Hashtable.Synchronized( new Hashtable() );
			autoCache = true;
		}

		public static void ClearCache()
		{
			cachedCRC32Tables.Clear();
		}

		private static uint Reflect(uint val)
		{
			uint oval = 0;
			for (int i=0; i<32; i++)
			{
				oval = (oval<<1) + (val&1);
				val >>= 1;
			}
			return oval;
		}

		protected static uint[] BuildCRC32Table( uint ulPolynomial )
		{
			uint dwCrc;
			uint[] table = new uint[256];

			ulPolynomial = Reflect(ulPolynomial);

			for (int i = 0; i < 256; i++)
			{
				dwCrc = (uint)i;
				for (int j = 8; j > 0; j--)
				{
					if((dwCrc & 1) == 1)
						dwCrc = (dwCrc >> 1) ^ ulPolynomial;
					else
						dwCrc >>= 1;
				}
				table[i] = dwCrc;
			}

			return table;
		}

        public CRC32(uint aPolynomial, bool cacheTable)
		{
			this.HashSizeValue = 32;

			crc32Table = (uint []) cachedCRC32Tables[aPolynomial];
			if ( crc32Table == null )
			{
				crc32Table = CRC32.BuildCRC32Table(aPolynomial);
				if ( cacheTable )
					cachedCRC32Tables.Add( aPolynomial, crc32Table );
			}
			Initialize();
		}
	
		public override void Initialize()
		{
			m_crc = AllOnes;
			this.State = 0;
		}
	
		protected override void HashCore(byte[] buffer, int offset, int count)
		{
			for (int i = offset; i < offset + count; i++)
			{
				ulong tabPtr = (m_crc & 0xFF) ^ buffer[i];
				m_crc >>= 8;
				m_crc ^= crc32Table[tabPtr];
			}

			this.State = 1;
		}
	
		protected override byte[] HashFinal()
		{
			byte [] finalHash = new byte [ 4 ];
			ulong finalCRC = m_crc ^ AllOnes;
		
			finalHash[0] = (byte) ((finalCRC >> 24) & 0xFF);
			finalHash[1] = (byte) ((finalCRC >> 16) & 0xFF);
			finalHash[2] = (byte) ((finalCRC >>  8) & 0xFF);
			finalHash[3] = (byte) ((finalCRC >>  0) & 0xFF);
		
			this.State = 0;
			return finalHash;
		}
	}
} // <T-bF7cOtc7M>
