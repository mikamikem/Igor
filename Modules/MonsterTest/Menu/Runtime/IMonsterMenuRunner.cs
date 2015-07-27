namespace Igor
{
	public interface IMonsterMenuRunner
	{
#if MONSTER_TEST_RUNTIME || UNITY_EDITOR
		void MonsterRunMenu(MonsterMenuNav MenuNavInst);
#endif // MONSTER_TEST_RUNTIME || UNITY_EDITOR
	}
}