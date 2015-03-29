namespace Igor
{
	public interface IIgorStepHandler
	{
		void RegisterJobStep(StepID CurrentStep, IIgorModule Module, IgorCore.JobStepFunc StepFunction);
	}
}