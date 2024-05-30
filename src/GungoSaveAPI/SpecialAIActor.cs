namespace SaveAPI;

public class SpecialAIActor : MonoBehaviour
{
    public bool SetsCustomFlagOnActivation;
    public CustomDungeonFlags CustomFlagToSetOnActivation;
    public bool SetsCustomFlagOnDeath;
    public CustomDungeonFlags CustomFlagToSetOnDeath;
    public bool SetsCustomCharacterSpecificFlagOnDeath;
    public CustomCharacterSpecificGungeonFlags CustomCharacterSpecificFlagToSetOnDeath;
}
