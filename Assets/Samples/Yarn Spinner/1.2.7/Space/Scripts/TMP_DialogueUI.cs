using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Yarn.Unity.Example
{
    public class TMP_DialogueUI : DialogueUI
    {
        public TMP_Text dialogueTextUI;
        
        // 代替父类的私有变量
        private bool userRequestedNextLine_Sub = false;
        
        /// Runs a line.
        /// <inheritdoc/>
        public override Dialogue.HandlerExecutionType RunLine (Yarn.Line line, ILineLocalisationProvider localisationProvider, System.Action onLineComplete)
        {
            // Start displaying the line; it will call onComplete later
            // which will tell the dialogue to continue
            StartCoroutine(DoRunLine(line, localisationProvider, onLineComplete));
            return Dialogue.HandlerExecutionType.PauseExecution;
        }

        /// Show a line of dialogue, gradually        
        private IEnumerator DoRunLine(Yarn.Line line, ILineLocalisationProvider localisationProvider, System.Action onComplete) {
            onLineStart?.Invoke();

            userRequestedNextLine_Sub = false;
            
            // The final text we'll be showing for this line.
            string text = localisationProvider.GetLocalisedTextForLine(line);

            if (text == null) {
                Debug.LogWarning($"Line {line.ID} doesn't have any localised text.");
                text = line.ID;
            }
            
            // 新增：一句话开始时就把文字全部设置到 TMP_Text 上，但设置可见字符数为 0
            dialogueTextUI.maxVisibleCharacters = 0;
            dialogueTextUI.text = text;
            yield return new WaitForEndOfFrame(); // wait for text to be parsed
            var charCount = dialogueTextUI.textInfo.characterCount;
            // Debug.Log(dialogueTextUI.GetParsedText());
            // Debug.Log(charCount);

            if (textSpeed > 0.0f) {
                // Display the line one character at a time
                for (var i = 0; i < charCount; i++) {
                    dialogueTextUI.maxVisibleCharacters = i + 1;
                    if (userRequestedNextLine_Sub) {
                        // We've requested a skip of the entire line.
                        // Display all of the text immediately.
                        dialogueTextUI.maxVisibleCharacters = int.MaxValue;
                        break;
                    }
                    yield return new WaitForSeconds (textSpeed);
                }
            } else {
                // Display the entire line immediately if textSpeed <= 0
                dialogueTextUI.maxVisibleCharacters = int.MaxValue;
            }

            // We're now waiting for the player to move on to the next line
            userRequestedNextLine_Sub = false;

            // Indicate to the rest of the game that the line has finished being delivered
            onLineFinishDisplaying?.Invoke();

            while (userRequestedNextLine_Sub == false) {
                yield return null;
            }

            // Avoid skipping lines if textSpeed == 0
            yield return new WaitForEndOfFrame();

            // Hide the text and prompt
            onLineEnd?.Invoke();

            onComplete();

        }
        
        public void MarkLineComplete_Sub() {
            userRequestedNextLine_Sub = true;
        }
    }
}
