namespace Igor
{
	public interface IMonsterDialogueRunner
	{
#if MONSTER_TEST_RUNTIME || UNITY_EDITOR
		void MonsterRunDialogue(MonsterDialogue DialogueInst);
#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
	}
}