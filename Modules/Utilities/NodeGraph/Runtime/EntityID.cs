#if IGOR_RUNTIME || UNITY_EDITOR
using UnityEngine;
using System.Collections;

namespace Igor
{
	[System.Serializable]
	public class EntityID {
		
		public bool IsEqual(EntityID Other)
		{
			if(Other == null)
			{
				return false;
			}
			
			UpdateIfNecessary();
			Other.UpdateIfNecessary();
			
			return ChapterFilename == Other.ChapterFilename && SceneFilename == Other.SceneFilename &&
				   DialogueFilename == Other.DialogueFilename && ConversationFilename == Other.ConversationFilename;
		}
		
		public EntityID()
		{
			SerializedVersion = ((int)TypeUtils.GlobalSerializationVersion.Version_Max)-1;
		}
		
		public int SerializedVersion = 0;
		public string ChapterFilename = "";
		public string SceneFilename = "";
		public string DialogueFilename = "";
		public string ConversationFilename = "";
		
		public IGraphEvent GetEntity()
		{
			UpdateIfNecessary();
			
	//		return ChapterManager.GetInstance().GetEntityForID(this);
			return null;
		}

		public EntityID Duplicate()
		{
			EntityID NewCopy = new EntityID();

			NewCopy.SerializedVersion = SerializedVersion;
			NewCopy.ChapterFilename = ChapterFilename;
			NewCopy.SceneFilename = SceneFilename;
			NewCopy.DialogueFilename = DialogueFilename;
			NewCopy.ConversationFilename = ConversationFilename;

			return NewCopy;
		}
		
		public void UpdateIfNecessary()
		{
			// Serialization fixups would go in here
		}
		
	}
}

#endif // IGOR_RUNTIME || UNITY_EDITOR
