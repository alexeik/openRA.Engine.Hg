using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRA.Platforms.Default
{

	public abstract class ShaderBase
	{
		public abstract void SetBool(string name, bool value);
		public abstract void SetVec(string name, float x);
		public abstract void SetVec(string name, float x, float y);
		public abstract void SetVec(string name, float x, float y, float z);
		public abstract void SetVec(string name, float[] vec, int length);
		public abstract void SetTexture(string param, ITexture texture);
		public abstract void SetMatrix(string param, float[] mtx);
		public abstract void PrepareRender();
		volatile int managedThreadId;

		protected ShaderBase()
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
