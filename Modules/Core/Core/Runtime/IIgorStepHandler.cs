using System;

namespace Igor
{
	public struct StepID : IComparable<StepID>
	{
		public string StepName;
		public int StepPriority;

		public StepID(string Name, int Priority)
		{
			StepName = Name;
			StepPriority = Priority;
		}

		public int CompareTo(StepID Other)
		{
			return StepPriority.CompareTo(Other.StepPriority);
		}
	}

	public interface IIgorStepHandler
	{
		void RegisterJobStep(StepID CurrentStep, IIgorModule Module, IgorRuntimeUtils.JobStepFunc StepFunction);
	}
}