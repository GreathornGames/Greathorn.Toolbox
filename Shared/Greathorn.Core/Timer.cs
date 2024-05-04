// Copyright Greathorn Games Inc. All Rights Reserved.

using System.Diagnostics;

namespace Greathorn.Core
{
	public class Timer
	{
		private readonly Stopwatch m_Stopwatch;
		public Timer()
		{
			m_Stopwatch = new Stopwatch();
			m_Stopwatch.Start();
		}

		public void Reset(bool restart = true)
		{
			m_Stopwatch.Reset();
			if (restart)
			{
				m_Stopwatch.Start();
			}
		}

		public long GetElapsedMilliseconds()
		{
			m_Stopwatch.Stop();
			return m_Stopwatch.ElapsedMilliseconds;
		}

		public long GetElapsedSeconds()
		{
			return GetElapsedMilliseconds() / 1000;
		}

		public string TransferRate(long byteCount)
		{
			float divisor = m_Stopwatch.ElapsedMilliseconds / 1000f;
			if (divisor == 0)
			{
				return string.Empty;
			}

			float speed = (byteCount / 125000f) / divisor;
			return $"{System.Math.Round(speed, 2)} Mbps";
		}
	}
}