using System.Collections.Generic;

namespace Doki.Renpie.RenDisco
{
    public interface IRuntimeEngine
    {
        void ShowDialogue(string character, string dialogue); // Display dialogue for a specific character
        void ShowNarration(string narration); // Display narration (no character)

        void ShowImage(string image, string transition = null); // Show an image with optional transition
        void HideImage(string image, string transition = null); // Hide image with optional transition

        void PlayMusic(string file, double? fadein); // Play music
        void StopMusic(double? fadeout); // Stop music
        void Pause(double? duration); // Pause music

        void ShowChoices(List<MenuChoice> choices); // Show choices to the user, and return the selected index

        void DefineCharacter(string id, Dictionary<string, string> settings); // Store, define and get Characters
        string GetCharacterColour(string id); // Retrieve character details (could be enhanced to return Character object)
        string GetCharacterName(string id); // Retrieve character name (could be enhanced to return Character object)

        void SetVariable(string name, object value); // Store, define and get variables
        object GetVariable(string name);

        void ExecuteDefine(Define define); // Handle the execution of Define commands directly
    }
}