using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRA.Platforms.Default
{
	public abstract class VertexBufferUserBase<T> : IDisposable
	{
		public abstract void BindOnceOpen();
		public abstract void SetData(T[] vertices, int length);
		public abstract void SetData(T[] vertices, int start, int length);
		public abstract void SetData(IntPtr data, int start, int length);
		public abstract void Dispose();
		public abstract void ActivateVAO();
		volatile int managedThreadId;

		protected VertexBufferUserBase()
		{
			SetThreadAffinity();
		}

		protected void SetThreadAffinity()
		{
			managedThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		protected void VerifyThreadAffinity()
		{
			if (managedThreadId != Thread.CurrentThread.ManagedThreadId)
				throw new InvalidOperationException("Cross-thread operation not valid: This method must only be called from the thread that owns this object.");
		}
	}
}
