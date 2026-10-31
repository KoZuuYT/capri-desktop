using System;
using System.IO;

namespace CapriDesktop;

internal static class Bunzip
{
	private sealed class Bits
	{
		private readonly Stream from;

		private readonly byte[] buffer = new byte[65536];

		private int at;

		private int end;

		private ulong held;

		private int count;

		internal Bits(Stream from)
		{
			this.from = from;
		}

		private int Next()
		{
			if (at == end)
			{
				at = 0;
				end = from.Read(buffer, 0, buffer.Length);
				if (end <= 0)
				{
					end = 0;
					return -1;
				}
			}
			return buffer[at++];
		}

		internal uint Read(int n)
		{
			while (count < n)
			{
				int num = Next();
				if (num < 0)
				{
					throw new IOException("The ears download ended early.");
				}
				held = (held << 8) | (uint)num;
				count += 8;
			}
			count -= n;
			return (uint)((int)(held >> count) & ((1 << n) - 1));
		}

		internal uint Bit()
		{
			return Read(1);
		}

		internal int NextByte()
		{
			count = 0;
			return Next();
		}
	}

	private static readonly uint[] Crcs = MakeCrcs();

	private static uint[] MakeCrcs()
	{
		uint[] array = new uint[256];
		for (uint num = 0u; num < 256; num++)
		{
			uint num2 = num << 24;
			for (int i = 0; i < 8; i++)
			{
				num2 = (((num2 & 0x80000000u) != 0) ? ((num2 << 1) ^ 0x4C11DB7) : (num2 << 1));
			}
			array[num] = num2;
		}
		return array;
	}

	private static IOException Damaged()
	{
		return new IOException("The ears download is damaged.");
	}

	internal static void Copy(Stream from, Stream to)
	{
		Bits bits = new Bits(from);
		byte[] output = new byte[65536];
		int num = 0;
		while (true)
		{
			int num2 = bits.NextByte();
			if (num2 != 66)
			{
				if (num > 0)
				{
					return;
				}
				throw Damaged();
			}
			if (bits.Read(8) != 90 || bits.Read(8) != 104)
			{
				throw Damaged();
			}
			int num3 = (int)(bits.Read(8) - 48);
			if (num3 < 1 || num3 > 9)
			{
				break;
			}
			Unpack(bits, num3 * 100000, output, to);
			num++;
		}
		throw Damaged();
	}

	private static void Unpack(Bits bits, int most, byte[] output, Stream to)
	{
		int[] array = new int[most];
		uint num = 0u;
		while (true)
		{
			uint num2 = bits.Read(24);
			uint num3 = bits.Read(24);
			if (num2 == 1536581 && num3 == 3690640)
			{
				if (((bits.Read(16) << 16) | bits.Read(16)) != num)
				{
					throw Damaged();
				}
				return;
			}
			if (num2 != 3227993 || num3 != 2511705)
			{
				throw Damaged();
			}
			uint num4 = (bits.Read(16) << 16) | bits.Read(16);
			if (bits.Bit() != 0)
			{
				throw new IOException("The ears download uses old bzip2 randomizing.");
			}
			int num5 = (int)bits.Read(24);
			byte[] array2 = new byte[256];
			int num6 = 0;
			uint num7 = bits.Read(16);
			for (int i = 0; i < 16; i++)
			{
				if ((num7 & (uint)(32768 >>> i)) == 0)
				{
					continue;
				}
				uint num8 = bits.Read(16);
				for (int j = 0; j < 16; j++)
				{
					if ((num8 & (uint)(32768 >>> j)) != 0)
					{
						array2[num6++] = (byte)(i * 16 + j);
					}
				}
			}
			if (num6 == 0)
			{
				throw Damaged();
			}
			int num9 = num6 + 2;
			int num10 = (int)bits.Read(3);
			if (num10 < 2 || num10 > 6)
			{
				throw Damaged();
			}
			int num11 = (int)bits.Read(15);
			if (num11 < 1)
			{
				throw Damaged();
			}
			byte[] array3 = new byte[6] { 0, 1, 2, 3, 4, 5 };
			byte[] array4 = new byte[num11];
			for (int k = 0; k < num11; k++)
			{
				int num12 = 0;
				while (bits.Bit() != 0)
				{
					if (++num12 >= num10)
					{
						throw Damaged();
					}
				}
				byte b = array3[num12];
				while (num12 > 0)
				{
					array3[num12] = array3[num12 - 1];
					num12--;
				}
				array3[0] = b;
				array4[k] = b;
			}
			int[][] array5 = new int[num10][];
			int[][] array6 = new int[num10][];
			int[][] array7 = new int[num10][];
			int[] array8 = new int[num10];
			for (int l = 0; l < num10; l++)
			{
				int[] array9 = new int[num9];
				int num13 = (int)bits.Read(5);
				for (int m = 0; m < num9; m++)
				{
					while (true)
					{
						if (num13 < 1 || num13 > 20)
						{
							throw Damaged();
						}
						if (bits.Bit() == 0)
						{
							break;
						}
						num13 += ((bits.Bit() == 0) ? 1 : (-1));
					}
					array9[m] = num13;
				}
				int num14 = 32;
				int num15 = 0;
				int[] array10 = array9;
				foreach (int num16 in array10)
				{
					if (num16 < num14)
					{
						num14 = num16;
					}
					if (num16 > num15)
					{
						num15 = num16;
					}
				}
				int[] array11 = new int[num9];
				int[] array12 = new int[23];
				int[] array13 = new int[23];
				int num17 = 0;
				for (int num18 = num14; num18 <= num15; num18++)
				{
					for (int num19 = 0; num19 < num9; num19++)
					{
						if (array9[num19] == num18)
						{
							array11[num17++] = num19;
						}
					}
				}
				array10 = array9;
				foreach (int num20 in array10)
				{
					array12[num20 + 1]++;
				}
				for (int num21 = 1; num21 < 23; num21++)
				{
					array12[num21] += array12[num21 - 1];
				}
				int num22 = 0;
				for (int num23 = num14; num23 <= num15; num23++)
				{
					num22 += array12[num23 + 1] - array12[num23];
					array13[num23] = num22 - 1;
					num22 <<= 1;
				}
				for (int num24 = num14 + 1; num24 <= num15; num24++)
				{
					array12[num24] = (array13[num24 - 1] + 1 << 1) - array12[num24];
				}
				array5[l] = array13;
				array6[l] = array12;
				array7[l] = array11;
				array8[l] = num14;
			}
			byte[] array14 = new byte[256];
			for (int num25 = 0; num25 < 256; num25++)
			{
				array14[num25] = (byte)num25;
			}
			int[] array15 = new int[256];
			int num26 = num6 + 1;
			int num27 = 0;
			int num28 = -1;
			int num29 = 0;
			int num30 = 0;
			int num31 = 1;
			int num32 = 0;
			int[] array16 = null;
			int[] array17 = null;
			int[] array18 = null;
			while (true)
			{
				if (num29 == 0)
				{
					if (++num28 >= num11)
					{
						throw Damaged();
					}
					num29 = 50;
					int num33 = array4[num28];
					array16 = array5[num33];
					array17 = array6[num33];
					array18 = array7[num33];
					num32 = array8[num33];
				}
				num29--;
				int num34 = num32;
				int num35;
				for (num35 = (int)bits.Read(num34); num35 > array16[num34]; num35 = (num35 << 1) | (int)bits.Bit())
				{
					if (++num34 > 20)
					{
						throw Damaged();
					}
				}
				int num36 = num35 - array17[num34];
				if (num36 < 0 || num36 >= num9)
				{
					throw Damaged();
				}
				int num37 = array18[num36];
				if (num37 <= 1)
				{
					if (num31 >= 2097152)
					{
						throw Damaged();
					}
					num30 += (num37 + 1) * num31;
					num31 <<= 1;
					continue;
				}
				if (num30 > 0)
				{
					if (num30 > most - num27)
					{
						throw Damaged();
					}
					byte b2 = array2[array14[0]];
					array15[b2] += num30;
					while (num30 > 0)
					{
						array[num27++] = b2;
						num30--;
					}
				}
				num31 = 1;
				if (num37 == num26)
				{
					break;
				}
				if (num27 >= most)
				{
					throw Damaged();
				}
				int num38 = num37 - 1;
				byte b3 = array14[num38];
				Array.Copy(array14, 0, array14, 1, num38);
				array14[0] = b3;
				array15[array2[b3]]++;
				array[num27++] = array2[b3];
			}
			if (num5 >= num27)
			{
				throw Damaged();
			}
			int[] array19 = new int[257];
			for (int num39 = 0; num39 < 256; num39++)
			{
				array19[num39 + 1] = array19[num39] + array15[num39];
			}
			for (int num40 = 0; num40 < num27; num40++)
			{
				array[array19[array[num40] & 0xFF]++] |= num40 << 8;
			}
			int num41 = array[num5] >> 8;
			int num42 = -1;
			int num43 = 0;
			int num44 = 0;
			uint num45 = uint.MaxValue;
			for (int num46 = 0; num46 < num27; num46++)
			{
				if ((uint)num41 >= (uint)num27)
				{
					throw Damaged();
				}
				int num47 = array[num41];
				int num48 = num47 & 0xFF;
				num41 = num47 >> 8;
				int num49 = 1;
				if (num43 == 4)
				{
					num49 = num48;
					num48 = num42;
					num43 = 0;
				}
				else if (num48 == num42)
				{
					num43++;
				}
				else
				{
					num42 = num48;
					num43 = 1;
				}
				while (num49 > 0)
				{
					num45 = (num45 << 8) ^ Crcs[(num45 >> 24) ^ (uint)num48];
					output[num44++] = (byte)num48;
					if (num44 == output.Length)
					{
						to.Write(output, 0, num44);
						num44 = 0;
					}
					num49--;
				}
			}
			if (num44 > 0)
			{
				to.Write(output, 0, num44);
			}
			if (~num45 != num4)
			{
				break;
			}
			num = ((num << 1) | (num >> 31)) ^ num4;
		}
		throw Damaged();
	}
}
