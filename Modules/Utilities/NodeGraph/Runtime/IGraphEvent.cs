#if IGOR_RUNTIME || UNITY_EDITOR
namespace Igor
{
	public interface IGraphEvent
	{
		// Returns true if the event is done updating, false if it should remain active for the next tick.
		bool ExecuteEvent();
		bool IsNextEventImmediate();
		bool IsEventImmediate();
		IGraphEvent GetNextEvent();
		IGraphEvent GetDefaultTriggeredEvent(); // This drills down until we reach a conversation node
		void ResetEvent();
	#if UNITY_EDITOR
		string GetLocationString();
		IGraphEvent[] GetAllNextPossibleEvents();
		void SetModifiedState(XMLSerializable.ModifiedState NewState, string VariableName);
		EntityID GenerateEntityIDForEvent();
	#endif // UNITY_EDITOR
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
