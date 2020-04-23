using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Graphics
{
	public enum SamplerType
	{
		Sampler,
		Sampler2d,

	}
	public class SamplerPointer
	{
		public SamplerType Stype1;
		public int num1;
		public SamplerType StypeSec;
		public int numSec;

	}
}
